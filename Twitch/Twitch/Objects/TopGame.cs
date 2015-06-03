﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using System.Collections.ObjectModel;
using System.Windows.Media.Imaging;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace TwitchAPIHandler.Objects
{
    public class TopGame
    {
        private int _channels;
        [DataMember]
        public int channels
        {
            get
            {
                return _channels;
            }
            set
            {
                if (value != _channels)
                {
                    _channels = value;
                }
            }
        }

        private Game _game;
        [DataMember]
        public Game game
        {
            get
            {
                return _game;
            }
            set
            {
                if (value != _game)
                {
                    _game = value;
                }
            }
        }

        public static string TOP_GAMES_PATH = PathStrings.TOP_GAMES_PATH;
        public static string NO_BOX_ART = PathStrings.NO_BOX_ART;

        public override bool Equals(object obj)
        {
            var item = obj as TopGame;

            if (item == null)
            {
                return false;
            }

            return this.game.Equals(item.game);
        }

        public override int GetHashCode()
        {
            return this.game.GetHashCode();
        }
    }

    public class Game
    {
        [DataMember]
        public string name { get; set; }
        [DataMember]
        public Box box { get; set; }

        public override bool Equals(object obj)
        {
            var item = obj as Game;

            if (item == null)
            {
                return false;
            }

            return this.name.Equals(item.name);
        }

        public override int GetHashCode()
        {
            return this.name.GetHashCode();
        }
    }

    public class Box
    {
        [DataMember]
        public BitmapImage medium { get; set; }
    }
}
