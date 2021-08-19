using Mono.Data.Sqlite;
using UnityEngine;
using System.Security.Cryptography;
using System.Data;

public class SQLiteManager : MonoBehaviour
{

    private string _dbName = "URI=file:Database.db";

    private void Start()
    {
        Create();

        AddAccount("gooby", "gooby");

        GetAccount();
    }

    void GetAccount()
    {

        using (var conn = new SqliteConnection(_dbName))
        {
            conn.Open();

            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT * FROM accounts;";

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        Debug.Log($"Username: {reader["username"]}+\nPassword: {reader["password"]}");
                    }

                    reader.Close();
                }
            }

            conn.Close();
        }
    }

    void AddAccount(string username, string password)
    {
        using (var conn = new SqliteConnection(_dbName))
        {
            conn.Open();

            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = $"INSERT INTO accounts (name, password) VALUES ('{username}','{password}');";
                cmd.ExecuteNonQuery();
            }

            conn.Close();
        }
    }

    void Create()
    {
        using (var conn = new SqliteConnection(_dbName))
        {
            conn.Open();

            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "CREATE TABLE IF NOT EXISTS accounts (name VARCHAR(20), password VARCHAR(20));";
                cmd.ExecuteNonQuery();
            }

            conn.Close();
        }
    }

}
