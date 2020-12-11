using System;
using System.Collections.Generic;
using System.IO;

namespace SharpSid
{
    public class EventList : List<Event>
    {
        public void AddEvent(Event ev)
        {
            if (ev != null && !this.Contains(ev))
            {
                this.Add(ev);
            }
        }

        public Event GetEventById(int id)
        {
            foreach (Event ev in this)
            {
                if (ev.m_id == id)
                {
                    return ev;
                }
            }

            return null;
        }
        public Event GetEventById(int id, EventScheduler context)
        {
            foreach (Event ev in this)
            {
                if (ev.m_id == id)
                {
                    return ev;
                }
            }

            if (context.m_id == id)
            {
                return context;
            }

            return null;
        }

        public bool ContainsID(int id)
        {
            foreach (Event ev in this)
            {
                if (ev.m_id == id)
                {
                    return true;
                }
            }

            return false;
        }

        public EventList()
            : base()
        {
        }
        // only used for deserializing
        public EventList(InternalPlayer player, EventScheduler context, BinaryReader reader)
            : base()
        {
            LoadFromReader(player, context, reader);
        }

        // serializing
        public void SaveToWriter(BinaryWriter writer)
        {
            writer.Write(this.Count);
            foreach (Event ev in this)
            {
                ev.SaveToWriter(writer);
            }
        }
        // deserializing
        protected void LoadFromReader(InternalPlayer player, EventScheduler context, BinaryReader reader)
        {
            int count = reader.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                int id = reader.ReadInt32();
                Event.EventType typ = (Event.EventType)reader.ReadInt16();
                switch (typ)
                {
                    case Event.EventType.ciaEvt:
                        this.Add(new CiaEvent(context, reader, id));
                        break;
                    case Event.EventType.cpuEvt:
                        this.Add(new CPUEvent(context, reader, id));
                        break;
                    case Event.EventType.GalwayEvt:
                        this.Add(new GalwayEvent(context, reader, id));
                        break;
                    case Event.EventType.mixerEvt:
                        this.Add(new EventMixer(player, context, reader, id));
                        break;
                    case Event.EventType.RtcEvt:
                        this.Add(new EventRTC(context, reader, id));
                        break;
                    case Event.EventType.SampleEvt:
                        this.Add(new SampleEvent(context, reader, id));
                        break;
                    case Event.EventType.TaEvt:
                        this.Add(new EventTa(context, reader, id));
                        break;
                    case Event.EventType.TbEvt:
                        this.Add(new EventTb(context, reader, id));
                        break;
                    case Event.EventType.TimeWarpEvt:
                        this.Add(new EventTimeWarp(context, reader, id));
                        break;
                    case Event.EventType.TodEvt:
                        this.Add(new EventTod(context, reader, id));
                        break;
                    case Event.EventType.vicEvt:
                        this.Add(new C64VIC(context.m_player, reader, id));
                        break;
                    case Event.EventType.xSidEvt:
                        this.Add(new xSIDEvent(context, reader, id));
                        break;
                    default:
#if DEBUG
                        throw new Exception("EventList.LoadFromReader: unknown Event id");
#else
                        break;
#endif

                }
            }

            context.m_next = GetEventById(context.m_next_id);
            context.m_prev = GetEventById(context.m_prev_id);

            foreach (Event ev in this)
            {
                ev.m_next = GetEventById(ev.m_next_id, context);
                ev.m_prev = GetEventById(ev.m_prev_id, context);

#if DEBUG
                if (ev.m_next_id > -1 && ev.m_next == null)
                {
                    throw new Exception("EventList.LoadFromReader: next Event id not found: " + ev.m_next_id.ToString());
                }
                if (ev.m_prev_id > -1 && ev.m_prev == null)
                {
                    throw new Exception("EventList.LoadFromReader: prev Event id not found: " + ev.m_prev_id.ToString());
                }
#endif
            }
        }

        public C64VIC GetVIC()
        {
            foreach (Event ev in this)
            {
                if (ev is C64VIC)
                {
                    return (C64VIC)ev;
                }
            }

#if DEBUG
            throw new Exception("EventList: C64VIC not found");
#else
            return null;
#endif
        }
        public EventMixer GetMixer()
        {
            foreach (Event ev in this)
            {
                if (ev is EventMixer)
                {
                    return (EventMixer)ev;
                }
            }

#if DEBUG
            throw new Exception("EventList: EventMixer not found");
#else
            return null;
#endif
        }
        public EventRTC GetRTC()
        {
            foreach (Event ev in this)
            {
                if (ev is EventRTC)
                {
                    return (EventRTC)ev;
                }
            }

#if DEBUG
            throw new Exception("EventList: EventRTC not found");
#else
            return null;
#endif
        }

        public static void SaveEvent2Writer(Event evt, BinaryWriter writer)
        {
            if (evt != null)
            {
                writer.Write(evt.m_id);
            }
            else
            {
                writer.Write((int)-1);
            }
        }
    }
}