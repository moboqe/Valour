﻿using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using System.Reflection;
using Valour.Api.Client;
using Valour.Api.Items;
using Valour.Api.Items.Channels;
using Valour.Api.Items.Messages;
using Valour.Shared;
using Valour.Shared.Items.Channels;

namespace Valour.Api.Nodes;

public class Node
{
    /// <summary>
    /// The name of this node
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// The HttpClient for this node. Should be configured to send requests only to this node
    /// </summary>
    public HttpClient HttpClient { get; set; }

    /// <summary>
    /// True if SignalR has hooked events
    /// </summary>
    public bool SignalREventsHooked { get; private set; }

    /// <summary>
    /// Hub connection for SignalR client
    /// </summary>
    public HubConnection HubConnection { get; private set; }

    /// <summary>
    /// The authentication token being used for this node
    /// </summary>
    public string Token { get; set; }

    /// <summary>
    /// True if this node is currently reconnecting
    /// </summary>
    public bool IsReconnecting { get; set; }


    public async Task InitializeAsync(string name, string token)
    {
        Name = name;
        Token = token;

        NodeManager.AddNode(this);

        HttpClient = new HttpClient();

        HttpClient.BaseAddress = new Uri(ValourClient.BaseAddress);

        // Set header for node
        HttpClient.DefaultRequestHeaders.Add("X-Server-Select", Name);
        HttpClient.DefaultRequestHeaders.Add("Authorization", Token);

        await Logger.Log($"[SignalR]: Setting up new hub for node '{Name}'");

        await ConnectSignalRHub();
        await AuthenticateSignalR();
        var userResult = await ConnectToUserSignalRChannel();

        if (!userResult.Success)
        {
            await Log("** Error connecting to user channel for SignalR. **");
            await Log(userResult.Message);
        }
        else
        {
            await Log("Connected to user channel for SignalR.");
        }
    }

    public async Task Log(string text, string color = "orange")
    {
        await Logger.Log($"[SignalR ({Name})]: " + text, color);
    }

    #region SignalR

    private async Task ConnectSignalRHub()
    {
        string address = ValourClient.BaseAddress + "planethub";

        await Logger.Log("Connecting to Planethub at " + address);

        HubConnection = new HubConnectionBuilder()
        .WithUrl(address, options =>
        {
            options.Headers.Add("X-Server-Select", Name);
        })
        .Build();

        HubConnection.ServerTimeout = TimeSpan.FromSeconds(20);

        HubConnection.Closed += OnSignalRClosed;
        HubConnection.Reconnected += OnSignalRReconnect;

        await HubConnection.StartAsync();

        await HookSignalREvents();
    }

    public async Task AuthenticateSignalR()
    {
        await Log("Authenticating with SignalR hub...");

        TaskResult response = new TaskResult(false, "Failed to authorize. This is a critical SignalR error.");

        bool authorized = false;
        int tries = 0;
        while (!authorized && tries < 5)
        {
            if (tries > 0)
                Thread.Sleep(3000);

            response = await HubConnection.InvokeAsync<TaskResult>("Authorize", Token);
            authorized = response.Success;
            tries++;
        }

        if (!authorized)
        {
            await Log("** FATAL: Failed to authorize with SignalR after 5 attempts. **");
        }

        await Log(response.Message);
    }

    public async Task HookSignalREvents()
    {
        await Logger.Log("[Item Events]: Hooking events.", "yellow");

        // For every single item...
        foreach (var type in Assembly.GetAssembly(typeof(Item)).GetTypes()
            .Where(x => x.IsClass && !x.IsAbstract && x.IsSubclassOf(typeof(Item))))
        {
            // Console.WriteLine(type.Name);

            // Register events

            HubConnection.On($"{type.Name}-Update", new Type[] { type, typeof(int) }, i => ValourClient.UpdateItem((dynamic)i[0], (int)i[1]));
            HubConnection.On($"{type.Name}-Delete", new Type[] { type }, i => ValourClient.DeleteItem((dynamic)i[0]));
        }

        HubConnection.On<PlanetMessage>("Relay", ValourClient.MessageRecieved);
        HubConnection.On<PlanetMessage>("DeleteMessage", ValourClient.MessageDeleted);
        HubConnection.On<ChannelStateUpdate>("Channel-State", ValourClient.UpdateChannelState);
        HubConnection.On<UserChannelState>("UserChannelState-Update", ValourClient.UpdateUserChannelState);

        await Logger.Log("[Item Events]: Events hooked.", "yellow");
    }

    /// <summary>
    /// Forces SignalR to refresh the underlying connection
    /// </summary>
    public async Task ForceRefresh()
    {
        await Log("Refresh has been requested.");

        // Send test ping
        await Log("SignalR state is " + HubConnection.State);

        if (HubConnection.State == HubConnectionState.Disconnected)
        {
            await Log("Disconnect has been detected. Reconnecting...");
            await Reconnect();
        }
    }

    /// <summary>
    /// Reconnects the SignalR connection
    /// </summary>
    public async Task Reconnect()
    {
        if (IsReconnecting)
            return;

        IsReconnecting = true;

        // Reconnect
        int tries = 0;

        // Test connection if it thinks it's safe
        if (HubConnection.State == HubConnectionState.Connected)
        {
            var ping = await HubConnection.InvokeAsync<string>("ping");
        }

        while (HubConnection.State == HubConnectionState.Disconnected)
        {
            await Task.Delay(3000);

            await Log("Reconnecting to Planet Hub...");

            try
            {
                await HubConnection.StartAsync();
            }
            catch (System.Exception)
            {
                await Log("Failed to reconnect... waiting three seconds to continue.", "red");
            }

            tries++;
        }

        await OnSignalRReconnect("Success");

        IsReconnecting = false;
    }

    /// <summary>
    /// Attempt to recover the connection if it is lost
    /// </summary>
    public async Task OnSignalRClosed(Exception e)
    {
        // Ensure disconnect was not on purpose
        if (e != null)
        {
            await Log("## A Breaking SignalR Error Has Occured", "red");
            await Log("Exception: " + e.Message, "red");
            await Log("Stacktrace: " + e.StackTrace, "red");

            await Reconnect();
        }
        else
        {
            await Log("SignalR has closed without error.");

            await Reconnect();
        }
    }

    /// <summary>
    /// Run when SignalR reconnects
    /// </summary>
    public  async Task OnSignalRReconnect(string data)
    {
        await Log("SignalR has reconnected. " + data, "lime");
        await HandleReconnect();

        await ValourClient.NotifyNodeReconnect(this);
    }

    /// <summary>
    /// Reconnects to SignalR systems when reconnected
    /// </summary>
    public async Task HandleReconnect()
    {
        // Authenticate and connect to personal channel
        await AuthenticateSignalR();
        await ConnectToUserSignalRChannel();

        foreach (var planet in ValourClient.OpenPlanets.Where(x => x.NodeName == Name))
        {
            await HubConnection.SendAsync("JoinPlanet", planet.Id);
            await Log($"Rejoined SignalR group for planet {planet.Id}", "lime");
        }

        foreach (var channel in ValourClient.OpenPlanetChannels.Where(x => x.NodeName == Name))
        {
            await HubConnection.SendAsync("JoinChannel", channel.Id);
            await Log($"Rejoined SignalR group for channel {channel.Id}", "lime");
        }
    }

    public async Task<TaskResult> ConnectToUserSignalRChannel()
    {
        return await HubConnection.InvokeAsync<TaskResult>("JoinUser");
    }

    #endregion

    #region HTTP Helpers

    /// <summary>
    /// Gets a json resource from the given uri and deserializes it
    /// </summary>
    public async Task<TaskResult<T>> GetJsonAsync<T>(string uri, bool allowNull = false)
        => await ValourClient.GetJsonAsync<T>(uri, allowNull, HttpClient);

    /// <summary>
    /// Gets a json resource from the given uri and deserializes it
    /// </summary>
    public async Task<TaskResult<string>> GetAsync(string uri)
        => await ValourClient.GetAsync(uri, HttpClient);

    /// <summary>
    /// Puts a string resource in the specified uri and returns the response message
    /// </summary>
    public async Task<TaskResult> PutAsync(string uri, string content)
        => await ValourClient.PutAsync(uri, content, HttpClient);

    /// <summary>
    /// Puts a json resource in the specified uri and returns the response message
    /// </summary>
    public async Task<TaskResult> PutAsync(string uri, object content)
        => await ValourClient.PutAsync(uri, content, HttpClient);

    /// <summary>
    /// Puts a json resource in the specified uri and returns the response message
    /// </summary>
    public async Task<TaskResult<T>> PutAsyncWithResponse<T>(string uri, T content)
        => await ValourClient.PutAsyncWithResponse<T>(uri, content, HttpClient);

    /// <summary>
    /// Posts a json resource in the specified uri and returns the response message
    /// </summary>
    public async Task<TaskResult> PostAsync(string uri, string content)
        => await ValourClient.PostAsync(uri, content, HttpClient);

    /// <summary>
    /// Posts a json resource in the specified uri and returns the response message
    /// </summary>
    public async Task<TaskResult> PostAsync(string uri, object content)
        => await ValourClient.PostAsync(uri, content, HttpClient);

    /// <summary>
    /// Posts a json resource in the specified uri and returns the response message
    /// </summary>
    public async Task<TaskResult<T>> PostAsyncWithResponse<T>(string uri, string content)
        => await ValourClient.PostAsyncWithResponse<T>(uri, content, HttpClient);

    /// <summary>
    /// Posts a json resource in the specified uri and returns the response message
    /// </summary>
    public async Task<TaskResult<T>> PostAsyncWithResponse<T>(string uri)
        => await ValourClient.PostAsyncWithResponse<T>(uri, HttpClient);

    /// <summary>
    /// Posts a multipart resource in the specified uri and returns the response message
    /// </summary>
    public async Task<TaskResult<T>> PostAsyncWithResponse<T>(string uri, MultipartFormDataContent content)
        => await ValourClient.PostAsyncWithResponse<T>(uri, content, HttpClient);


    /// <summary>
    /// Posts a json resource in the specified uri and returns the response message
    /// </summary>
    public async Task<TaskResult<T>> PostAsyncWithResponse<T>(string uri, object content)
        => await ValourClient.PostAsyncWithResponse<T>(uri, content, HttpClient);

    /// <summary>
    /// Deletes a resource in the specified uri and returns the response message
    /// </summary>
    public async Task<TaskResult> DeleteAsync(string uri)
        => await ValourClient.DeleteAsync(uri, HttpClient);

    #endregion
}
