﻿using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations.Schema;
using Valour.Database.Items.Users;
using Valour.Shared.Oauth;

namespace Valour.Database.Items.Authorization;

public class ServerAuthToken : AuthToken
{
    public static ConcurrentDictionary<string, ServerAuthToken> QuickCache = new ConcurrentDictionary<string, ServerAuthToken>();

    [ForeignKey("User_Id")]
    public virtual ServerUser User { get; set; }

    /// <summary>
    /// Will return the auth object for a valid token, including the user.
    /// This will log the access time in the user object.
    /// A null response means the token was invalid.
    /// </summary>
    public static async Task<ServerAuthToken> TryAuthorize(string token, ValourDB db)
    {
        if (token == null) return null;

        ServerAuthToken authToken = null;

        if (QuickCache.ContainsKey(token))
        {
            authToken = QuickCache[token];
        }
        else
        {
            authToken = await db.AuthTokens.FindAsync(token);

            QuickCache.TryAdd(token, authToken);
        }

        // Spin off a task to do things we don't want to wait on
        var t = Task.Run(async () =>
        {
            using (ValourDB tdb = new ValourDB(ValourDB.DBOptions))
            {
                if (authToken == null)
                {
                    authToken = await tdb.AuthTokens.FindAsync(token);
                }

                if (authToken != null)
                {
                    ServerUser user = await tdb.Users.FindAsync(authToken.User_Id);
                    user.Last_Active = DateTime.UtcNow;

                    await tdb.SaveChangesAsync();
                }
            }
        });


        return authToken;
    }
}

