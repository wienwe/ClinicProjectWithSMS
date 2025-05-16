using Npgsql;
using System;
using System.Collections.Generic;

namespace PolyclinicApp
{
    public class DatabaseHelper
    {
        public string ConnectionString { get; }

        public DatabaseHelper(string host, string database, string username, string password)
        {
            ConnectionString = $"Host={host};Database={database};Username={username};Password={password}";
        }

        public void InitializeDatabase()
        {
            try
            {
                using var conn = new NpgsqlConnection(ConnectionString);
                conn.Open();

                using var cmd = new NpgsqlCommand { Connection = conn };

                cmd.CommandText = @"CREATE TABLE IF NOT EXISTS users (
                                user_id SERIAL PRIMARY KEY,
                                full_name VARCHAR(100) NOT NULL,
                                phone VARCHAR(20) NOT NULL UNIQUE,
                                gender VARCHAR(10) NOT NULL,
                                birth_date DATE NOT NULL)";
                cmd.ExecuteNonQuery();

                cmd.CommandText = @"CREATE TABLE IF NOT EXISTS doctors (
                                doctor_id SERIAL PRIMARY KEY,
                                name VARCHAR(100) NOT NULL UNIQUE,
                                specialization VARCHAR(100) NOT NULL)";
                cmd.ExecuteNonQuery();

                cmd.CommandText = @"CREATE TABLE IF NOT EXISTS schedule (
                                schedule_id SERIAL PRIMARY KEY,
                                doctor_id INTEGER NOT NULL REFERENCES doctors(doctor_id),
                                time TIME NOT NULL,
                                is_available BOOLEAN DEFAULT TRUE,
                                UNIQUE (doctor_id, time))";
                cmd.ExecuteNonQuery();

                cmd.CommandText = @"CREATE TABLE IF NOT EXISTS appointments (
                                appointment_id SERIAL PRIMARY KEY,
                                user_id INTEGER NOT NULL REFERENCES users(user_id),
                                schedule_id INTEGER NOT NULL REFERENCES schedule(schedule_id),
                                created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                                UNIQUE (user_id, schedule_id))";
                cmd.ExecuteNonQuery();

                cmd.CommandText = @"INSERT INTO doctors (name, specialization) VALUES 
                                ('Иванов И.И.', 'Терапевт'),
                                ('Петров П.П.', 'Хирург'),
                                ('Сидорова С.С.', 'Офтальмолог')
                                ON CONFLICT (name) DO NOTHING";
                cmd.ExecuteNonQuery();

                var times = new[] { "08:00", "10:00", "12:00", "14:00", "16:00", "18:00", "20:00" };
                foreach (var time in times)
                {
                    for (int i = 1; i <= 3; i++)
                    {
                        cmd.CommandText = @"INSERT INTO schedule (doctor_id, time) 
                                       VALUES (@doctorId, @time)
                                       ON CONFLICT (doctor_id, time) DO NOTHING";
                        cmd.Parameters.Clear();
                        cmd.Parameters.AddWithValue("@doctorId", i);
                        cmd.Parameters.AddWithValue("@time", TimeSpan.Parse(time));
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при инициализации БД: {ex.Message}");
                throw;
            }
        }

        public int RegisterUser(string fullName, string phone, string gender, DateTime birthDate)
        {
            using var conn = new NpgsqlConnection(ConnectionString);
            conn.Open();

            using var cmd = new NpgsqlCommand { Connection = conn };
            cmd.CommandText = @"INSERT INTO users (full_name, phone, gender, birth_date) 
                             VALUES (@fullName, @phone, @gender, @birthDate) 
                             RETURNING user_id";
            cmd.Parameters.AddWithValue("@fullName", fullName);
            cmd.Parameters.AddWithValue("@phone", phone);
            cmd.Parameters.AddWithValue("@gender", gender);
            cmd.Parameters.AddWithValue("@birthDate", birthDate);

            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        public int? AuthenticateUser(string phone)
        {
            using var conn = new NpgsqlConnection(ConnectionString);
            conn.Open();

            using var cmd = new NpgsqlCommand { Connection = conn };
            cmd.CommandText = "SELECT user_id FROM users WHERE phone = @phone";
            cmd.Parameters.AddWithValue("@phone", phone);

            var result = cmd.ExecuteScalar();
            return result != null ? Convert.ToInt32(result) : null;
        }

        public List<Doctor> GetDoctors()
        {
            var doctors = new List<Doctor>();

            using var conn = new NpgsqlConnection(ConnectionString);
            conn.Open();

            using var cmd = new NpgsqlCommand("SELECT doctor_id, name, specialization FROM doctors", conn);
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                doctors.Add(new Doctor
                {
                    Id = reader.GetInt32(0),
                    Name = reader.GetString(1),
                    Specialization = reader.GetString(2)
                });
            }

            return doctors;
        }

        public List<ScheduleSlot> GetAvailableSlots(int doctorId)
        {
            var slots = new List<ScheduleSlot>();

            using var conn = new NpgsqlConnection(ConnectionString);
            conn.Open();

            using var cmd = new NpgsqlCommand { Connection = conn };
            cmd.CommandText = @"SELECT s.schedule_id, s.time 
                             FROM schedule s
                             LEFT JOIN appointments a ON s.schedule_id = a.schedule_id
                             WHERE s.doctor_id = @doctorId AND a.schedule_id IS NULL
                             ORDER BY s.time";
            cmd.Parameters.AddWithValue("@doctorId", doctorId);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                slots.Add(new ScheduleSlot
                {
                    Id = reader.GetInt32(0),
                    Time = reader.GetTimeSpan(1).ToString(@"hh\:mm")
                });
            }

            return slots;
        }

        public bool IsTimeSlotBooked(int doctorId, string time)
        {
            using var conn = new NpgsqlConnection(ConnectionString);
            conn.Open();

            using var cmd = new NpgsqlCommand { Connection = conn };
            cmd.CommandText = @"SELECT COUNT(*) 
                             FROM appointments a
                             JOIN schedule s ON a.schedule_id = s.schedule_id
                             WHERE s.doctor_id = @doctorId AND s.time = @time";
            cmd.Parameters.AddWithValue("@doctorId", doctorId);
            cmd.Parameters.AddWithValue("@time", TimeSpan.Parse(time));

            return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
        }

        public bool CreateAppointment(int userId, int scheduleId)
        {
            using var conn = new NpgsqlConnection(ConnectionString);
            conn.Open();

            using var cmd = new NpgsqlCommand { Connection = conn };
            cmd.CommandText = "INSERT INTO appointments (user_id, schedule_id) VALUES (@userId, @scheduleId)";
            cmd.Parameters.AddWithValue("@userId", userId);
            cmd.Parameters.AddWithValue("@scheduleId", scheduleId);

            try
            {
                cmd.ExecuteNonQuery();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public UserInfo? GetUserInfo(int userId)
        {
            using var conn = new NpgsqlConnection(ConnectionString);
            conn.Open();

            using var cmd = new NpgsqlCommand { Connection = conn };
            cmd.CommandText = "SELECT full_name, phone FROM users WHERE user_id = @userId";
            cmd.Parameters.AddWithValue("@userId", userId);

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                return new UserInfo
                {
                    FullName = reader.GetString(0),
                    Phone = reader.GetString(1)
                };
            }

            return null;
        }

        public AppointmentInfo? GetAppointmentInfo(int scheduleId)
        {
            using var conn = new NpgsqlConnection(ConnectionString);
            conn.Open();

            using var cmd = new NpgsqlCommand { Connection = conn };
            cmd.CommandText = @"SELECT a.appointment_id, d.name, d.specialization, s.time 
                              FROM appointments a
                              JOIN schedule s ON a.schedule_id = s.schedule_id
                              JOIN doctors d ON s.doctor_id = d.doctor_id
                              WHERE a.schedule_id = @scheduleId";
            cmd.Parameters.AddWithValue("@scheduleId", scheduleId);

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                return new AppointmentInfo
                {
                    AppointmentId = reader.GetInt32(0),
                    DoctorName = reader.GetString(1),
                    Specialization = reader.GetString(2),
                    AppointmentTime = reader.GetTimeSpan(3).ToString(@"hh\:mm")
                };
            }

            return null;
        }
    }
}