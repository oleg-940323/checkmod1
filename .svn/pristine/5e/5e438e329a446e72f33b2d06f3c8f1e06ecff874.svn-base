using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace checkmod
{

    public class RecMulti
    {

        public bool forever = true;

        UdpClient client;

        public RecMulti(IPEndPoint ip)
        {
            _ip = ip;
        }
        private IPEndPoint _ip;


        public void CloseThread()
        {
            forever = false;
        }

        public void RecieveMulticastData(Module m)
        {
            object locker = new object();

            // Буфер приема 
            byte[] data;
            try
            {
                client = new UdpClient(Header.PortMulti + (m.pr.ident_collect[(byte)SeqIdentDataInCollect.position] as IdentByte).value);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

            if (client != null) 
            {
                IPEndPoint ip =  new IPEndPoint(m.ip.Address, Header.PortMulti + (m.pr.ident_collect[(byte)SeqIdentDataInCollect.position] as IdentByte).value);
                client.JoinMulticastGroup(HeaderDriver.ip_multi_rec);
                client.Client.ReceiveTimeout = 1;

                while (forever)
                {

                    try
                    {
                        data = client.Receive(ref ip);

                        // Проверка на тип кадра
                        switch (data[1])
                        {

                            /* Кадр с данными */
                            case (byte)type_frame_enum.fModSig:

                                m.pr.sign.ParsData(data);
                                break;
                        }
                    }
                    catch
                    {
                    }
                }
                if (client != null)
                    client.Close();
                client = null;
            }
            else
                MessageBox.Show("Сокет не создан");
        }
    }
}
