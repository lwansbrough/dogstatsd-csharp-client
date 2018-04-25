using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace StatsdClient
{
    public interface IStatsd
    {
        List<string> Commands { get; }

        void Send<TCommandType, T>(string name, T value, double sampleRate, params string[] tags) where TCommandType : StatsdClient.Statsd.Metric;
        Task SendAsync<TCommandType, T>(string name, T value, double sampleRate, params string[] tags) where TCommandType : StatsdClient.Statsd.Metric;

        void Add<TCommandType, T>(string name, T value, double sampleRate, params string[] tags) where TCommandType : StatsdClient.Statsd.Metric;

        void Send(string title, string text, string alertType, string aggregationKey, string sourceType, int? dateHappened, string priority, string hostname, string[] tags, bool truncateIfTooLong = false);
        Task SendAsync(string title, string text, string alertType, string aggregationKey, string sourceType, int? dateHappened, string priority, string hostname, string[] tags, bool truncateIfTooLong = false);

        void Add(string title, string text, string alertType, string aggregationKey, string sourceType, int? dateHappened, string priority, string hostname, string[] tags, bool truncateIfTooLong = false);

        void Send(string command);
        Task SendAsync(string command);
        
        void Send();
        Task SendAsync();

        void Add(Action actionToTime, string statName, double sampleRate, params string[] tags);

        void Send(Action actionToTime, string statName, double sampleRate, params string[] tags);
        Task SendAsync(Action actionToTime, string statName, double sampleRate, params string[] tags);
        /// <summary>
        /// Add service check
        /// </summary>
        void Add(string name, int status, int? timestamp, string hostname, string[] tags, string serviceCheckMessage, bool truncateIfTooLong);
        /// <summary>
        /// Send service check
        /// </summary>
        void Send(string name, int status, int? timestamp, string hostname, string[] tags, string serviceCheckMessage, bool truncateIfTooLong);
        Task SendAsync(string name, int status, int? timestamp, string hostname, string[] tags, string serviceCheckMessage, bool truncateIfTooLong);
    }
}
