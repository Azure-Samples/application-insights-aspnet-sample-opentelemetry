using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Microsoft.ApplicationInsights;

namespace Sample.RabbitMQCollector.ApplicationInsights
{
    public class RabbitMQCollector : IObserver<DiagnosticListener>
    {
        private readonly TelemetryClient client;
        private long disposed;
        private List<IDisposable> listenerSubscriptions;
        private IDisposable allSourcesSubscription;


        public RabbitMQCollector(TelemetryClient client)
        {
            this.client = client;
            this.listenerSubscriptions = new List<IDisposable>();
        }

        public void Subscribe()
        {
            if (this.allSourcesSubscription == null)
            {
                this.allSourcesSubscription = DiagnosticListener.AllListeners.Subscribe(this);
            }
        }

        public void OnCompleted()
        {
        }

        public void OnError(Exception error)
        {
        }

        public void OnNext(DiagnosticListener value)
        {
            if ((Interlocked.Read(ref this.disposed) == 0))
            {
                if (value.Name == "Sample.RabbitMQ")
                {
                    var listener = new RabbitMQSourceListener(client);
                    var subscription = value.Subscribe(listener);

                    lock (this.listenerSubscriptions)
                    {
                        this.listenerSubscriptions.Add(subscription);
                    }
                }
            }
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref this.disposed, 1) == 1)
            {
                // already disposed
                return;
            }

            lock (this.listenerSubscriptions)
            {
                foreach (var listenerSubscription in this.listenerSubscriptions)
                {
                    listenerSubscription?.Dispose();
                }

                this.listenerSubscriptions.Clear();
            }

            this.allSourcesSubscription?.Dispose();
            this.allSourcesSubscription = null;
        }
    }
}
