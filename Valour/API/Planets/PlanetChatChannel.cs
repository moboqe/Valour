﻿using Valour.Api.Client;
using Valour.Api.Messages;
using Valour.Shared;
using Valour.Shared.Roles;

namespace Valour.Api.Planets;


/*  Valour - A free and secure chat client
*  Copyright (C) 2021 Vooper Media LLC
*  This program is subject to the GNU Affero General Public license
*  A copy of the license should be included - if not, see <http://www.gnu.org/licenses/>
*/

public class PlanetChatChannel : Shared.Items.PlanetChatChannel
{
    /// <summary>
    /// Returns the channel for the given id
    /// </summary>
    public static async Task<TaskResult<PlanetChatChannel>> FindAsync(ulong id, bool force_refresh = false)
    {
        if (!force_refresh && ValourCache.Channels.ContainsKey(id))
            return new TaskResult<PlanetChatChannel>(true, "Success: Cached", ValourCache.Channels[id]);

        var getResponse = await ValourClient.GetJsonAsync<PlanetChatChannel>($"api/channel/{id}");

        if (getResponse.Success)
            ValourCache.Channels[id] = getResponse.Data;

        return getResponse;
    }

    /// <summary>
    /// Returns the name of the item type
    /// </summary>
    public string GetItemTypeName() => "Chat Channel";

    /// <summary>
    /// Attempts to delete the channel
    /// </summary>
    public async Task<TaskResult> DeleteAsync() => 
        await ValourClient.DeleteAsync($"api/channel/{Id}");

    /// <summary>
    /// Returns the planet this channel belongs to
    /// </summary>
    public async Task<TaskResult<Planet>> GetPlanetAsync() => 
        await Planet.FindAsync(Planet_Id);

    /// <summary>
    /// Sets the description of this channel
    /// </summary>
    public async Task<TaskResult> SetDescriptionAsync(string desc) => 
        await ValourClient.PutAsync($"api/channel/{Id}/description", desc);

    /// <summary>
    /// Sets if the permissions should be inherited from the parent category
    /// </summary>
    public async Task<TaskResult> SetPermInheritModeAsync(bool value) => 
        await ValourClient.PutAsync($"api/channel/{Id}/inherits_perms", value);

    /// <summary>
    /// Sets the parent category id of the channel
    /// </summary>
    public async Task<TaskResult> SetParentIdAsync(ulong id) => 
        await ValourClient.PutAsync($"api/channel/{Id}/parent_id", id);

    /// <summary>
    /// Returns the permissions node for the given role id
    /// </summary>
    public async Task<TaskResult<PermissionsNode>> GetPermissionsNodeAsync(ulong role_id) {
        var res = await GetChannelPermissionsNodeAsync(role_id);
        return new TaskResult<PermissionsNode>(res.Success, res.Message, res.Data);
    }

    /// <summary>
    /// Returns the channel permissions node for the given role id
    /// </summary>
    public async Task<TaskResult<ChatChannelPermissionsNode>> GetChannelPermissionsNodeAsync(ulong role_id) =>
        await ValourClient.GetJsonAsync<ChatChannelPermissionsNode>($"api/node/channel/{Id}/{role_id}");

    /// <summary>
    /// Returns the last (count) messages starting at (index)
    /// </summary>
    public async Task<TaskResult<List<PlanetMessage>>> GetMessagesAsync(ulong index = ulong.MaxValue, int count = 10) =>
        await ValourClient.GetJsonAsync<List<PlanetMessage>>($"api/channel/{Id}/messages?index={index}&count={count}");

    /// <summary>
    /// Returns the last (count) messages
    /// </summary>
    public async Task<TaskResult<List<PlanetMessage>>> GetLastMessagesAsync(int count = 10) =>
        await ValourClient.GetJsonAsync<List<PlanetMessage>>($"api/channel/{Id}/messages?count={count}");
}

