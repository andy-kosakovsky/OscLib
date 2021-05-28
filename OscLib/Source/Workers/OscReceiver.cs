using System;
using System.Net;
using System.Collections.Generic;
using System.Text;

namespace OscLib
{
    public class OscReceiver
    {
        // TODO: add bundle delay thing here.
        protected OscLink _link;
        protected OscConverter _converter;


        #region EVENTS

        public MessageHandler MessageReceived;

        public BundleHandler BundleReceived;

        public BadDataHandler BadDataReceived;

        #endregion // EVENTS


        public void Connect(OscLink link, OscConverter converter)
        {
            _link = link;
            _converter = converter;

            _link.PacketReceived += ReceivePacket;
        }


        public void Disconnect()
        {
            _link.PacketReceived -= ReceivePacket;

            _link = null;
            _converter = null;
        }


        protected virtual void ReceivePacket<Packet>(Packet packet, IPEndPoint endPoint) where Packet : IOscPacket
        {
            if (packet[0] == OscProtocol.BundleMarker)
            {
                BundleReceived?.Invoke(_converter.GetBundle(packet), endPoint);
            }
            else if (packet[0] == OscProtocol.Separator)
            {
                MessageReceived?.Invoke(_converter.GetMessage(packet), endPoint);
            }
            else
            {
                BadDataReceived?.Invoke(packet.GetBytes(), endPoint);
            }

        }

    }

}
