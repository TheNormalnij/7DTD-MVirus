﻿using System.Collections.Generic;

namespace MVirus.Server.NetStreams
{
    internal class OutgoingStreamStore
    {
        private readonly Dictionary<ClientInfo, List<OutcomingNetStreamHandler>> store = new Dictionary<ClientInfo, List<OutcomingNetStreamHandler>>();

        public OutgoingStreamStore() { }

        public void Add(ClientInfo client, OutcomingNetStreamHandler handler)
        {
            List<OutcomingNetStreamHandler> clientStreams;
            
            if (!store.TryGetValue(client, out clientStreams))
            {
                clientStreams = new List<OutcomingNetStreamHandler>();
                store[client] = clientStreams;
            }

            clientStreams.Add(handler);
        }

        public List<ClientInfo> GetActiveClients()
        {
            var list = new List<ClientInfo>();
            store.CopyKeysTo(list);
            return list;
        }

        public OutcomingNetStreamHandler GetClientStream(ClientInfo client, byte streamId)
        {
            List<OutcomingNetStreamHandler> clientStreams;

            if (!store.TryGetValue(client, out clientStreams))
                return null;

            foreach (var handler in clientStreams)
            {
                if (handler.streamId == streamId)
                    return handler;
            }

            return null;
        }

        public OutcomingNetStreamHandler[] GetStreams(ClientInfo client)
        {
            List<OutcomingNetStreamHandler> streams;

            if (!store.TryGetValue(client, out streams)) return new OutcomingNetStreamHandler[0];

            return streams.ToArray();
        }

        public void Remove(ClientInfo client)
        {
            store.Remove(client);
        }

        public void Remove(ClientInfo client, byte streamId)
        {
            List<OutcomingNetStreamHandler> clientStreams;

            if (!store.TryGetValue(client, out clientStreams))
                return;

            foreach (var handler in clientStreams)
            {
                if (handler.streamId == streamId)
                {
                    clientStreams.Remove(handler);
                    return;
                }
            }
        }

        public void Remove(ClientInfo client, OutcomingNetStreamHandler stream)
        {
            List<OutcomingNetStreamHandler> clientStreams;

            if (!store.TryGetValue(client, out clientStreams))
                return;

            clientStreams.Remove(stream);

            if (clientStreams.Count == 0)
                store.Remove(client);
        }
    }
}
