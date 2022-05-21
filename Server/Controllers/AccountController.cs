using System.Security.Cryptography;
using ayayay.Shared;
using Comforix.Shared;
using DatabaseAccess;
using Microsoft.AspNetCore.Mvc;

namespace Comforix.Server.Controllers;

[ApiController]
[Route("AccountApi")]
public class AccountController : ControllerBase
{
    private readonly string ConnectionString = "Server=127.0.0.1;Port=3306;Database=comforix;Uid=comforix_db;Pwd=aKrL72CMwomSkoMzvufz3CfmuUzuWT5o";

    [HttpPost]
    [Route("New")]
    public async Task<ActionResult> NewAcc(Account account)
    {
        IAccess database = new Access();

        // Check if username is taken
        const string check = "SELECT * FROM accounts WHERE Username = @Username LIMIT 1";
        List<Account> result = await database.QueryAsync<Account, dynamic>(check, new
        {
            account.Username
        });
        if (result.Count is 1) return Conflict("Username already exists");

        // Store new account into database
        const string command =
            "INSERT INTO accounts (Username, PasswordHash, PasswordSalt, Level1, Level2, Level3, Reputation) VALUES (@Username, @PasswordHash, @PasswordSalt, @Level1, @Level2, @Level3, @Reputation)";
        await database.ExecuteAsync(command, new
        {
            account.Username,
            account.PasswordHash,
            account.PasswordSalt,
            account.Level1,
            account.Level2,
            account.Level3,
            Reputation = 9
        });

        // Create token
        string token = await TokenUtils.NewToken(account.Username);
        return Ok(token);
    }

    [HttpPost]
    [Route("Login")]
    public async Task<ActionResult> LoginToAcc(LoginReq request)
    {
        // Check if account exists. If exists, retrieve
        IAccess access = new Access();
        const string sql = "SELECT * FROM accounts WHERE Username = @Username LIMIT 1";
        List<Account> accounts = await access.QueryAsync<Account, dynamic>(sql, new
        {
            Username = request.Username
        });
        if (accounts.Count is not 1) return Unauthorized();
        Account selected = accounts.First();

        // Salt
        byte[] hash = request.Password.Concat(selected.PasswordHash).ToArray();
        
        // Hash
        using var hasher = SHA512.Create();
        hash = hasher.ComputeHash(hash);
        
        // Compare
        if (hash.SequenceEqual(selected.PasswordHash) is false) return Unauthorized();
        string token = await TokenUtils.NewToken(selected.Username);
        return Ok(token);
    }
}