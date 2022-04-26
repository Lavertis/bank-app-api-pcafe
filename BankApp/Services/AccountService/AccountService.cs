using AutoMapper;
using BankApp.Data;
using BankApp.Entities;
using BankApp.Exceptions;
using BankApp.Exceptions.RequestErrors;
using BankApp.Models.Requests;
using Microsoft.EntityFrameworkCore;

namespace BankApp.Services.AccountService;

public class AccountService : IAccountService
{
    private readonly ApplicationDbContext _dbContext;

    public AccountService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IEnumerable<Account>> GetAllAccountsAsync()
    {
        var accounts = await _dbContext.Accounts.ToListAsync();
        return accounts;
    }

    public async Task<bool> IsCustomerAccountOwnerAsync(string userId, long accountId)
    {
        var customer = await _dbContext.Customers.FindAsync(userId);
        if (customer == null)
        {
            throw new AppException("Customer with requested id could not be found");
        }

        var result = customer.BankAccounts.Exists(e => e.Id == accountId);
        return result;
    }

    public async Task<Account> GetAccountByIdAsync(long id)
    {
        var account = await _dbContext.Accounts.FindAsync(id);
        if (account == null)
            throw new NotFoundError("Id", "Account with requested id could not be found");
        return account;
    }

    public async Task<Account> CreateAccountAsync(CreateAccountRequest request)
    {
        await using (var dbContextTransaction = await _dbContext.Database.BeginTransactionAsync())
        {
            var accountType = await _dbContext.AccountTypes.FindAsync(request.AccountTypeId);
            var currency = await _dbContext.Currencies.FindAsync(request.CurrencyId);
            var customer = await _dbContext.Customers.FindAsync(request.CustomerId);
            if (accountType == null)
                throw new AppException("Account type with requested id could not be found");
            if (currency == null)
                throw new AppException("Currency with requested id could not be found");
            if (customer == null)
                throw new AppException("Customer with requested id could not be found");
            var mapper = new Mapper(
                new MapperConfiguration(cfg =>
                    cfg.CreateMap<CreateAccountRequest, Account>()
                )
            );

            var account = mapper.Map<Account>(request);
            account.AccountType = accountType;
            account.Currency = currency;

            long accountNumber;
            try
            {
                accountNumber = _dbContext.Accounts.Select(acc => acc.Id).Max() + 1;
            }
            catch (InvalidOperationException)
            {
                accountNumber = 1;
            }

            var id = accountNumber.ToString();
            var paddedId = id.PadLeft(16, '0');
            account.Number = paddedId;
            account.IsActive = request.IsActive;
            customer.BankAccounts.Add(account);

            await _dbContext.SaveChangesAsync();
            await dbContextTransaction.CommitAsync();

            return account;
        }
    }

    public async Task<Account> UpdateAccountAsync(UpdateAccountRequest request, long id)
    {
        var account = await GetAccountByIdAsync(id);
        account.Balance = request.Balance ?? account.Balance;
        account.TransferLimit = request.TransferLimit ?? account.TransferLimit;
        account.IsActive = request.IsActive ?? account.IsActive;
        await _dbContext.SaveChangesAsync();
        return account;
    }

    public async Task<bool> DeleteAccountAsync(long id)
    {
        var account = await GetAccountByIdAsync(id);
        _dbContext.Accounts.Remove(account);
        await _dbContext.SaveChangesAsync();
        return true;
    }
}