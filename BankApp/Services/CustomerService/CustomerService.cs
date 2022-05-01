﻿using AutoMapper;
using BankApp.Data;
using BankApp.Entities.UserTypes;
using BankApp.Exceptions;
using BankApp.Models;
using BankApp.Models.Requests;
using BankApp.Services.UserService;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BankApp.Services.CustomerService;

public class CustomerService : ICustomerService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IUserService _userService;

    public CustomerService(ApplicationDbContext dbContext, IUserService userService)
    {
        _dbContext = dbContext;
        _userService = userService;
    }

    public async Task<IList<Customer>> GetAllCustomersAsync()
    {
        var employees = await _dbContext.Customers.ToListAsync();
        return employees;
    }

    public async Task<Customer> GetCustomerByIdAsync(string customerId)
    {
        var customer = await _dbContext.Customers.FindAsync(customerId);
        if (customer == null)
            throw new NotFoundException("Customer with requested id does not exist");
        return customer;
    }

    public async Task<Customer> CreateCustomerAsync(CreateCustomerRequest request)
    {
        var user = await _userService.CreateUserAsync(request.UserName, request.Password, RoleType.Customer);

        var mapper = new Mapper(new MapperConfiguration(cfg =>
            cfg.CreateMap<CreateCustomerRequest, Customer>()
        ));
        var customer = mapper.Map<Customer>(request);
        customer.AppUser = user;

        var customerEntity = (await _dbContext.Customers.AddAsync(customer)).Entity;
        await _dbContext.SaveChangesAsync();
        return customerEntity;
    }

    public async Task<Customer> UpdateCustomerByIdAsync(UpdateCustomerRequest request, string customerId)
    {
        var hasher = new PasswordHasher<AppUser>();
        var customer = await GetCustomerByIdAsync(customerId);
        customer.AppUser.UserName = request.UserName;
        customer.AppUser.NormalizedUserName = request.UserName.ToUpperInvariant();
        customer.AppUser.PasswordHash = hasher.HashPassword(null!, request.Password);
        customer.FirstName = request.FirstName;
        customer.MiddleName = request.SecondName;
        customer.LastName = request.LastName;
        await _dbContext.SaveChangesAsync();
        return customer;
    }

    public async Task<bool> DeleteCustomerByIdAsync(string customerId)
    {
        return await _userService.DeleteUserByIdAsync(customerId);
    }

    public async Task<bool> CustomerExistsByIdAsync(string customerId)
    {
        var exists = await _dbContext.Customers.AnyAsync(c => c.Id == customerId);
        return exists;
    }

    public async Task<bool> NationalIdExistsAsync(string nationalId)
    {
        var exists = await _dbContext.Customers.AnyAsync(c => c.NationalId == nationalId);
        return exists;
    }
}