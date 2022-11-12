using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RLNetInspector.Model
{
    public class RLNetPaketModel
    {
        public ObservableCollection<WSPacket> RLNetPackets { get; set; } = new ObservableCollection<WSPacket>();
        public RLNetPaketModel()
        {
        }

        public void AddRequest(string PsyNetID, string data)
        {
            if (data == null)
                return;

            foreach (WSPacket packet in RLNetPackets)
            {
                if(packet.PsyNetID == PsyNetID)
                {
                    packet.RequestData = data;
                    return;
                }
            }

            App.Current.Dispatcher.Invoke((Action)delegate
            {
                RLNetPackets.Add(new WSPacket
                {
                    PsyNetID = PsyNetID,
                    RequestData = data
                });
            });

        }
        public void AddResponse(string PsyNetID, string data)
        {
            if (data == null)
                return;

            foreach (WSPacket packet in RLNetPackets)
            {
                if (packet.PsyNetID == PsyNetID)
                {
                    packet.ResponseData = data;
                    return;
                }
            }

            App.Current.Dispatcher.Invoke((Action)delegate
            {
                RLNetPackets.Add(new WSPacket
                {
                    PsyNetID = PsyNetID,
                    ResponseData = data
                });
            });
        }
        public void AddPing(string PsyNetID, string data)
        {
            if (data == null)
                return;

            foreach (WSPacket packet in RLNetPackets)
            {
                if (packet.PsyNetID == PsyNetID)
                {
                    packet.RequestData = data;
                    return;
                }
            }

            App.Current.Dispatcher.Invoke((Action)delegate
            {
                RLNetPackets.Add(new WSPacket
                {
                    PsyNetID = PsyNetID,
                    RequestData = data
                });
            });
        }
        public void AddPong(string PsyNetID, string data)
        {
            if (data == null)
                return;

            foreach (WSPacket packet in RLNetPackets)
            {
                if (packet.PsyNetID == PsyNetID)
                {
                    packet.ResponseData = data;
                    return;
                }
            }

            App.Current.Dispatcher.Invoke((Action)delegate
            {
                RLNetPackets.Add(new WSPacket
                {
                    PsyNetID = PsyNetID,
                    ResponseData = data
                });
            });
        }
        public void Remove(WSPacket packet)
        {
            if (packet == null)
                return;

            App.Current.Dispatcher.Invoke((Action)delegate
            {
                RLNetPackets.Remove(packet);
            });
        }

    }
}
