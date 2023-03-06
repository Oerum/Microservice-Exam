﻿using Auth.Database;
using Discord;
using Discord.WebSocket;
using DiscordBot.Application.Interface;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Crosscutting.DiscordConnectionHandler.DiscordClientLibrary;
using Crosscutting.TransactionHandling;

namespace DiscordBot.Infrastructure;

public class DiscordBotCleanupRepository : IDiscordBotCleanupRepository
{
    private readonly AuthDbContext _db;
    private readonly ILogger<DiscordBotCleanupRepository> _logger;
    private readonly DiscordSocketClient _client = DiscordClient.GetDiscordSocketClient();
    private readonly IConfiguration _configuration;
    private readonly IUnitOfWork<AuthDbContext> _unitOfWork;

    public DiscordBotCleanupRepository(AuthDbContext db, ILogger<DiscordBotCleanupRepository> logger, IConfiguration configuration, IUnitOfWork<AuthDbContext> unitOfWork)
    {
        _db = db;
        _logger = logger;
        _configuration = configuration;
        _unitOfWork = unitOfWork;
    }

    public async Task CleanUp()
    {
        try
        {
            var expiredLicenses = await _db.ActiveLicenses
                .Include(user => user.User)
                .Where(time => DateTime.Now >= time.EndDate).ToListAsync();

            var allActiveLicenses = await _db.ActiveLicenses.Include(user => user.User)
                .Where(time => DateTime.Now < time.EndDate).ToListAsync();

            if (expiredLicenses.Any())
            {
                _db.RemoveRange(expiredLicenses);
                await _db.SaveChangesAsync();
            }

            try
            {
                var guild = _client.GetGuild(ulong.Parse(_configuration["Discord:Guid"]!));
                var guildMembers = await guild.GetUsersAsync().FlattenAsync();


                foreach (var member in guildMembers.Where(x => x.RoleIds.ToList().Count > 1))
                {
                    IGuildUser? guildUser = null;

                    if (guild != null)
                    {
                        var clientUser = await _client.GetUserAsync(member.Id);
                        guildUser = guild.GetUser(clientUser.Id);
                    }

                    if (guildUser != null && (expiredLicenses.Exists(x => x.User.DiscordId == member.Id.ToString())
                        || !allActiveLicenses.Exists(user => user.User.DiscordId == member.Id.ToString())))
                    {
                        var rolesNotToRemove = new HashSet<ulong>
                        {
                            860603152280584222, //Everyone
                            860603777790771211, //Mod
                            860628656259203092, //Staff
                            860907247017394190 //Server Booster
                        };
                        var roles = guildUser.RoleIds.Where(id => !rolesNotToRemove.Contains(id)).ToList();

                        try
                        {
                            await guildUser.RemoveRolesAsync(roles);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogInformation("Failed to remove role: " + ex.Message + $"\n{guildUser.Username + "#" + guildUser.Discriminator}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogInformation("Role Removal Exception: " + ex.Message);
            }
                

            _logger.LogInformation("Finished Roles & DB Purge");
        }
        catch (Exception ex)
        {
            _logger.LogInformation("Unexpected error: " + ex.Message);
        }
    }
}