﻿using System;
using System.IO;
using System.Text;
using JetBrains.Annotations;

namespace fNbt {
    /// <summary> BinaryReader wrapper that takes care of reading primitives from an NBT stream,
    /// while taking care of endianness, string encoding, and skipping. </summary>
    internal sealed class NbtBinaryReader : BinaryReader {
        readonly byte[] floatBuffer = new byte[sizeof( float )],
                        doubleBuffer = new byte[sizeof( double )];

        byte[] seekBuffer;
        const int SeekBufferSize = 8 * 1024;
        readonly bool swapNeeded;
        readonly byte[] stringConversionBuffer = new byte[64];


        public NbtBinaryReader( [NotNull] Stream input, bool bigEndian )
            : base( input ) {
            swapNeeded = ( BitConverter.IsLittleEndian == bigEndian );
        }


        public NbtTagType ReadTagType() {
            var type = (NbtTagType)ReadByte();
            if( type < NbtTagType.End || type > NbtTagType.IntArray ) {
                throw new NbtFormatException( "NBT tag type out of range: " + (int)type );
            }
            return type;
        }


        public override short ReadInt16() {
            if( swapNeeded ) {
                return NbtBinaryWriter.Swap( base.ReadInt16() );
            } else {
                return base.ReadInt16();
            }
        }


        public override int ReadInt32() {
            if( swapNeeded ) {
                return NbtBinaryWriter.Swap( base.ReadInt32() );
            } else {
                return base.ReadInt32();
            }
        }


        public override long ReadInt64() {
            if( swapNeeded ) {
                return NbtBinaryWriter.Swap( base.ReadInt64() );
            } else {
                return base.ReadInt64();
            }
        }


        public override float ReadSingle() {
            if( swapNeeded ) {
                BaseStream.Read( floatBuffer, 0, sizeof( float ) );
                Array.Reverse( floatBuffer );
                return BitConverter.ToSingle( floatBuffer, 0 );
            }
            return base.ReadSingle();
        }


        public override double ReadDouble() {
            if( swapNeeded ) {
                BaseStream.Read( doubleBuffer, 0, sizeof( double ) );
                Array.Reverse( doubleBuffer );
                return BitConverter.ToDouble( doubleBuffer, 0 );
            }
            return base.ReadDouble();
        }


        public override string ReadString() {
            short length = ReadInt16();
            if( length < 0 ) {
                throw new NbtFormatException( "Negative string length given!" );
            }
            if( length < stringConversionBuffer.Length ) {
                BaseStream.Read( stringConversionBuffer, 0, length );
                return Encoding.UTF8.GetString( stringConversionBuffer, 0, length );
            } else {
                byte[] stringData = ReadBytes( length );
                return Encoding.UTF8.GetString( stringData );
            }
        }


        public void Skip( int bytesToSkip ) {
            if( bytesToSkip < 0 ) {
                throw new ArgumentOutOfRangeException( "bytesToSkip" );
            } else if( BaseStream.CanSeek ) {
                BaseStream.Position += bytesToSkip;
            } else if( bytesToSkip != 0 ) {
                if( seekBuffer == null )
                    seekBuffer = new byte[SeekBufferSize];
                int bytesDone = 0;
                while( bytesDone < bytesToSkip ) {
                    int readThisTime = BaseStream.Read( seekBuffer, bytesDone, bytesToSkip - bytesDone );
                    if( readThisTime == 0 ) {
                        throw new EndOfStreamException();
                    }
                    bytesDone += readThisTime;
                }
            }
        }


        public void SkipString() {
            short length = ReadInt16();
            if( length < 0 ) {
                throw new NbtFormatException( "Negative string length given!" );
            }
            Skip( length );
        }


        public TagSelector Selector { get; set; }
    }
}