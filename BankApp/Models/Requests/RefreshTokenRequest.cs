﻿namespace BankApp.Models.Requests;

public class RefreshTokenRequest
{
    public string RefreshToken { get; set; } = string.Empty;
}