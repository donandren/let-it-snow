﻿using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace AvaloniaCoreSnow
{
    public class SnowViewModel : INotifyPropertyChanged
    {
        private int _flakeCount = 3000;

        private Flake[] _flakes;

        private readonly Random _rnd = new Random();
        
        private readonly Action _invalidate;

        public SnowViewModel(Action invalidate)
        {
            _invalidate = invalidate;

            // TODO: Pixel format affects perf?
            Bitmap = new WritableBitmap(640, 480, PixelFormat.Bgra8888);

            InitFlakes();
            Task.Run(() => MoveFlakes());
        }

        public WritableBitmap Bitmap { get; }

        public int FlakeCount
        {
            get => _flakeCount;
            set
            {
                _flakeCount = value;
                OnPropertyChanged(nameof(FlakeCount));
            }
        }

        private void InitFlakes()
        {
            _flakes = new Flake[_flakeCount];

            for (var i = 0; i < _flakes.Length; i++)
            {
                ref var f = ref _flakes[i];
                InitFlake(ref f);
                f.Y = (short) _rnd.Next(40);
                f.Y2 = 0;
            }
        }

        private void InitFlake(ref Flake f)
        {
            var tone = (byte) _rnd.Next(200);
            f.Color = GetGray((byte) (tone + 50));
            f.X = (short) _rnd.Next(Bitmap.PixelWidth);
            f.Speed = tone;
        }

        private unsafe void MoveFlakes()
        {
            const short slowdown = 200;

            while (true)
            {
                var bmp = Bitmap;
                var w = bmp.PixelWidth;
                var h = bmp.PixelHeight;
                using (var buf = bmp.Lock())
                {
                    var ptr = (uint*) buf.Address;

                    for (var i = 0; i < _flakes.Length; i++)
                    {
                        ref var f = ref _flakes[i];

                        f.Y2 += f.Speed;

                        if (f.Y2 > slowdown)
                        {
                            // Erase old flake.
                            *(ptr + w * f.Y + f.X) = 0;

                            // Draw new.
                            f.Y2 = (short) (f.Y2 % slowdown);
                            f.Y++;
                            if (f.Y >= h)
                            {
                                f.Y = 0;
                                InitFlake(ref f);
                            }

                            *(ptr + w * f.Y + f.X) = f.Color;
                        }
                    }
                }

                _invalidate();
                Thread.Sleep(10);
            }
            // ReSharper disable once FunctionNeverReturns
        }

        private uint GetGray(byte tone)
        {
            return (uint) (tone | tone << 8 | tone << 16 | 0xFF000000);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private struct Flake
        {
            public short X;
            public short Y;
            public short Y2;
            public uint Color;
            public byte Speed;
        }
    }
}
