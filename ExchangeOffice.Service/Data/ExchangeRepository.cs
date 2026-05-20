using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;

namespace ExchangeOffice.Service.Data
{
    public class BalanceDto
    {
        public string CurrencyCode { get; set; }
        public decimal Amount { get; set; }
    }

    public class TransactionDto
    {
        public int TransactionId { get; set; }
        public int UserId { get; set; }
        public string Type { get; set; }
        public string CurrencyCode { get; set; }
        public decimal Amount { get; set; }
        public decimal Rate { get; set; }
        public decimal PlnAmount { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class ExchangeRepository
    {
        private readonly string _connectionString;

        public ExchangeRepository()
        {
            _connectionString = ConfigurationManager.ConnectionStrings["ExchangeOfficeDb"]?.ConnectionString
                ?? throw new InvalidOperationException("Connection string 'ExchangeOfficeDb' not found in config.");
        }

        public int CreateUser(string fullName)
        {
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "INSERT INTO dbo.Users (FullName) VALUES (@FullName); SELECT SCOPE_IDENTITY();";
                cmd.Parameters.AddWithValue("@FullName", fullName);
                conn.Open();
                var idObj = cmd.ExecuteScalar();
                return Convert.ToInt32(idObj);
            }
        }

        public int RegisterUser(string fullName, string username, string passwordHash)
        {
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "INSERT INTO dbo.Users (FullName, Username, PasswordHash) VALUES (@FullName, @Username, @PasswordHash); SELECT SCOPE_IDENTITY();";
                cmd.Parameters.AddWithValue("@FullName", fullName);
                cmd.Parameters.AddWithValue("@Username", username);
                cmd.Parameters.AddWithValue("@PasswordHash", passwordHash);
                conn.Open();
                var idObj = cmd.ExecuteScalar();
                return Convert.ToInt32(idObj);
            }
        }

        public int LoginUser(string username, string passwordHash)
        {
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT UserId FROM dbo.Users WHERE Username = @Username AND PasswordHash = @PasswordHash";
                cmd.Parameters.AddWithValue("@Username", username);
                cmd.Parameters.AddWithValue("@PasswordHash", passwordHash);
                conn.Open();
                var res = cmd.ExecuteScalar();
                if (res == null)
                    return -1;
                return Convert.ToInt32(res);
            }
        }

        public bool UsernameExists(string username)
        {
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT COUNT(1) FROM dbo.Users WHERE Username = @Username";
                cmd.Parameters.AddWithValue("@Username", username);
                conn.Open();
                return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
            }
        }

        public bool UserExists(int userId)
        {
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT COUNT(1) FROM dbo.Users WHERE UserId = @UserId";
                cmd.Parameters.AddWithValue("@UserId", userId);
                conn.Open();
                return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
            }
        }

        public decimal TopUpBalance(int userId, string currencyCode, decimal amount)
        {
            using (var conn = new SqlConnection(_connectionString))
            using (var tran = conn.BeginTransactionWrapper())
            {
                conn.Open();
                tran.Begin();
                try
                {
                    using (var cmdU = conn.CreateCommand())
                    {
                        cmdU.Transaction = tran.Transaction;
                        cmdU.CommandText = "SELECT COUNT(1) FROM dbo.Users WHERE UserId = @UserId";
                        cmdU.Parameters.AddWithValue("@UserId", userId);
                        var exists = Convert.ToInt32(cmdU.ExecuteScalar()) > 0;
                        if (!exists) throw new InvalidOperationException($"User with id {userId} does not exist.");
                    }

                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.Transaction = tran.Transaction;
                        cmd.CommandText = @"IF EXISTS (SELECT 1 FROM dbo.Balances WHERE UserId = @UserId AND CurrencyCode = @CurrencyCode)
    UPDATE dbo.Balances SET Amount = Amount + @Amount WHERE UserId = @UserId AND CurrencyCode = @CurrencyCode
ELSE
    INSERT INTO dbo.Balances (UserId, CurrencyCode, Amount) VALUES (@UserId, @CurrencyCode, @Amount);
SELECT Amount FROM dbo.Balances WHERE UserId = @UserId AND CurrencyCode = @CurrencyCode;";
                        cmd.Parameters.AddWithValue("@UserId", userId);
                        cmd.Parameters.AddWithValue("@CurrencyCode", currencyCode);
                        cmd.Parameters.AddWithValue("@Amount", amount);
                        var result = cmd.ExecuteScalar();

                        using (var cmdT = conn.CreateCommand())
                        {
                            cmdT.Transaction = tran.Transaction;
                            cmdT.CommandText = @"INSERT INTO dbo.Transactions (UserId, Type, CurrencyCode, Amount, Rate, PlnAmount)
VALUES (@UserId, 'TOP_UP', @CurrencyCode, @Amount, @Rate, @PlnAmount);";
                            cmdT.Parameters.AddWithValue("@UserId", userId);
                            cmdT.Parameters.AddWithValue("@CurrencyCode", currencyCode);
                            cmdT.Parameters.AddWithValue("@Amount", amount);
                            cmdT.Parameters.AddWithValue("@Rate", currencyCode == "PLN" ? 1m : 0m);
                            cmdT.Parameters.AddWithValue("@PlnAmount", currencyCode == "PLN" ? Math.Round(amount, 2) : 0m);
                            cmdT.ExecuteNonQuery();
                        }

                        tran.Commit();
                        return Convert.ToDecimal(result);
                    }
                }
                catch
                {
                    tran.Rollback();
                    throw;
                }
            }
        }

        public List<BalanceDto> GetUserBalances(int userId)
        {
            var list = new List<BalanceDto>();
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"SELECT CurrencyCode, Amount FROM dbo.Balances WHERE UserId = @UserId";
                cmd.Parameters.AddWithValue("@UserId", userId);
                conn.Open();
                using (var rdr = cmd.ExecuteReader())
                {
                    while (rdr.Read())
                    {
                        list.Add(new BalanceDto
                        {
                            CurrencyCode = rdr.GetString(0),
                            Amount = rdr.GetDecimal(1)
                        });
                    }
                }
            }
            return list;
        }

        public List<TransactionDto> GetTransactionHistory(int userId)
        {
            var list = new List<TransactionDto>();
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"SELECT TransactionId, UserId, Type, CurrencyCode, Amount, Rate, PlnAmount, CreatedAt
FROM dbo.Transactions WHERE UserId = @UserId ORDER BY TransactionId";
                cmd.Parameters.AddWithValue("@UserId", userId);
                conn.Open();
                using (var rdr = cmd.ExecuteReader())
                {
                    while (rdr.Read())
                    {
                        list.Add(new TransactionDto
                        {
                            TransactionId = rdr.GetInt32(0),
                            UserId = rdr.GetInt32(1),
                            Type = rdr.GetString(2),
                            CurrencyCode = rdr.GetString(3),
                            Amount = rdr.GetDecimal(4),
                            Rate = rdr.GetDecimal(5),
                            PlnAmount = rdr.GetDecimal(6),
                            CreatedAt = rdr.GetDateTime(7)
                        });
                    }
                }
            }
            return list;
        }

        public decimal BuyCurrency(int userId, string currencyCode, decimal foreignAmount, decimal rate)
        {
            using (var conn = new SqlConnection(_connectionString))
            using (var tran = conn.BeginTransactionWrapper())
            {
                conn.Open();
                tran.Begin();
                try
                {
                    // check user exists
                    using (var cmdU = conn.CreateCommand())
                    {
                        cmdU.Transaction = tran.Transaction;
                        cmdU.CommandText = "SELECT COUNT(1) FROM dbo.Users WHERE UserId = @UserId";
                        cmdU.Parameters.AddWithValue("@UserId", userId);
                        var exists = Convert.ToInt32(cmdU.ExecuteScalar()) > 0;
                        if (!exists) throw new InvalidOperationException($"User with id {userId} does not exist.");
                    }

                    var requiredPln = Math.Round(foreignAmount * rate, 2);

                    decimal plnBal = 0m;
                    using (var cmdP = conn.CreateCommand())
                    {
                        cmdP.Transaction = tran.Transaction;
                        cmdP.CommandText = "SELECT Amount FROM dbo.Balances WHERE UserId=@UserId AND CurrencyCode='PLN'";
                        cmdP.Parameters.AddWithValue("@UserId", userId);
                        var obj = cmdP.ExecuteScalar();
                        plnBal = obj == null ? 0m : Convert.ToDecimal(obj);
                        if (plnBal < requiredPln) throw new InvalidOperationException($"Insufficient PLN balance. Required: {requiredPln:0.00}, Available: {plnBal:0.00}");
                    }

                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.Transaction = tran.Transaction;
                        cmd.CommandText = @"UPDATE dbo.Balances SET Amount = Amount - @Amount WHERE UserId=@UserId AND CurrencyCode='PLN'";
                        cmd.Parameters.AddWithValue("@Amount", requiredPln);
                        cmd.Parameters.AddWithValue("@UserId", userId);
                        cmd.ExecuteNonQuery();
                    }

                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.Transaction = tran.Transaction;
                        cmd.CommandText = @"IF EXISTS (SELECT 1 FROM dbo.Balances WHERE UserId=@UserId AND CurrencyCode=@Currency)
    UPDATE dbo.Balances SET Amount = Amount + @AddAmt WHERE UserId=@UserId AND CurrencyCode=@Currency
ELSE
    INSERT INTO dbo.Balances (UserId, CurrencyCode, Amount) VALUES (@UserId, @Currency, @AddAmt);
SELECT Amount FROM dbo.Balances WHERE UserId=@UserId AND CurrencyCode=@Currency;";
                        cmd.Parameters.AddWithValue("@UserId", userId);
                        cmd.Parameters.AddWithValue("@Currency", currencyCode);
                        cmd.Parameters.AddWithValue("@AddAmt", foreignAmount);
                        var newAmount = Convert.ToDecimal(cmd.ExecuteScalar());

                        using (var cmdT = conn.CreateCommand())
                        {
                            cmdT.Transaction = tran.Transaction;
                            cmdT.CommandText = @"INSERT INTO dbo.Transactions (UserId, Type, CurrencyCode, Amount, Rate, PlnAmount)
VALUES (@UserId, 'BUY', @Currency, @Amount, @Rate, @PlnAmount);";
                            cmdT.Parameters.AddWithValue("@UserId", userId);
                            cmdT.Parameters.AddWithValue("@Currency", currencyCode);
                            cmdT.Parameters.AddWithValue("@Amount", foreignAmount);
                            cmdT.Parameters.AddWithValue("@Rate", rate);
                            cmdT.Parameters.AddWithValue("@PlnAmount", requiredPln);
                            cmdT.ExecuteNonQuery();
                        }

                        tran.Commit();
                        return newAmount;
                    }
                }
                catch
                {
                    tran.Rollback();
                    throw;
                }
            }
        }

        public decimal SellCurrency(int userId, string currencyCode, decimal foreignAmount, decimal rate)
        {
            using (var conn = new SqlConnection(_connectionString))
            using (var tran = conn.BeginTransactionWrapper())
            {
                conn.Open();
                tran.Begin();
                try
                {
                    using (var cmdU = conn.CreateCommand())
                    {
                        cmdU.Transaction = tran.Transaction;
                        cmdU.CommandText = "SELECT COUNT(1) FROM dbo.Users WHERE UserId = @UserId";
                        cmdU.Parameters.AddWithValue("@UserId", userId);
                        var exists = Convert.ToInt32(cmdU.ExecuteScalar()) > 0;
                        if (!exists) throw new InvalidOperationException($"User with id {userId} does not exist.");
                    }

                    decimal currBal = 0m;
                    using (var cmdC = conn.CreateCommand())
                    {
                        cmdC.Transaction = tran.Transaction;
                        cmdC.CommandText = "SELECT Amount FROM dbo.Balances WHERE UserId=@UserId AND CurrencyCode=@Currency";
                        cmdC.Parameters.AddWithValue("@UserId", userId);
                        cmdC.Parameters.AddWithValue("@Currency", currencyCode);
                        var obj = cmdC.ExecuteScalar();
                        currBal = obj == null ? 0m : Convert.ToDecimal(obj);
                        if (currBal < foreignAmount) throw new InvalidOperationException($"Insufficient {currencyCode} balance. Requested: {foreignAmount:0.00}, Available: {currBal:0.00}");
                    }

                    var plnToCredit = Math.Round(foreignAmount * rate, 2);

                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.Transaction = tran.Transaction;
                        cmd.CommandText = "UPDATE dbo.Balances SET Amount = Amount - @Amount WHERE UserId=@UserId AND CurrencyCode=@Currency";
                        cmd.Parameters.AddWithValue("@Amount", foreignAmount);
                        cmd.Parameters.AddWithValue("@UserId", userId);
                        cmd.Parameters.AddWithValue("@Currency", currencyCode);
                        cmd.ExecuteNonQuery();
                    }

                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.Transaction = tran.Transaction;
                        cmd.CommandText = @"IF EXISTS (SELECT 1 FROM dbo.Balances WHERE UserId=@UserId AND CurrencyCode='PLN')
    UPDATE dbo.Balances SET Amount = Amount + @AddAmt WHERE UserId=@UserId AND CurrencyCode='PLN'
ELSE
    INSERT INTO dbo.Balances (UserId, CurrencyCode, Amount) VALUES (@UserId, 'PLN', @AddAmt);
SELECT Amount FROM dbo.Balances WHERE UserId=@UserId AND CurrencyCode='PLN';";
                        cmd.Parameters.AddWithValue("@UserId", userId);
                        cmd.Parameters.AddWithValue("@AddAmt", plnToCredit);
                        var newPln = Convert.ToDecimal(cmd.ExecuteScalar());

                        using (var cmdT = conn.CreateCommand())
                        {
                            cmdT.Transaction = tran.Transaction;
                            cmdT.CommandText = @"INSERT INTO dbo.Transactions (UserId, Type, CurrencyCode, Amount, Rate, PlnAmount)
VALUES (@UserId, 'SELL', @Currency, @Amount, @Rate, @PlnAmount);";
                            cmdT.Parameters.AddWithValue("@UserId", userId);
                            cmdT.Parameters.AddWithValue("@Currency", currencyCode);
                            cmdT.Parameters.AddWithValue("@Amount", foreignAmount);
                            cmdT.Parameters.AddWithValue("@Rate", rate);
                            cmdT.Parameters.AddWithValue("@PlnAmount", plnToCredit);
                            cmdT.ExecuteNonQuery();
                        }

                        tran.Commit();
                        return newPln;
                    }
                }
                catch
                {
                    tran.Rollback();
                    throw;
                }
            }
        }
    }

    // Helper to manage SqlTransaction with using syntax
    internal class TransactionWrapper : IDisposable
    {
        private readonly SqlConnection _conn;
        public SqlTransaction Transaction { get; private set; }
        public TransactionWrapper(SqlConnection conn)
        {
            _conn = conn;
        }
        public void Begin()
        {
            Transaction = _conn.BeginTransaction();
        }
        public void Commit()
        {
            Transaction?.Commit();
            Transaction = null;
        }
        public void Rollback()
        {
            try { Transaction?.Rollback(); } catch { }
            Transaction = null;
        }
        public void Dispose()
        {
            if (Transaction != null)
            {
                try { Transaction.Rollback(); } catch { }
                Transaction = null;
            }
        }
    }

    internal static class SqlConnectionExtensions
    {
        public static TransactionWrapper BeginTransactionWrapper(this SqlConnection conn)
        {
            return new TransactionWrapper(conn);
        }
    }
}
