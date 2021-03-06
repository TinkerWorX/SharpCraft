﻿using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using EasyHook;
using TinkerWorX.SharpCraft.Windows;
using System.Text;
using System.Diagnostics;

namespace TinkerWorX.SharpCraft.Utilities
{
    public static class Memory
    {
        private static List<LocalHook> hooks = new List<LocalHook>();

        public static T Read<T>(IntPtr address) where T : struct
        {
            return (T)Marshal.PtrToStructure(address, typeof(T));
        }

        public static T Read<T>(IntPtr address, Int32 offset) where T : struct
        {
            return (T)Marshal.PtrToStructure(address + offset, typeof(T));
        }

        public static String ReadString(IntPtr address)
        {
            return Marshal.PtrToStringAnsi(address);
        }

        public static String ReadString(IntPtr address, Int32 length)
        {
            return Marshal.PtrToStringAnsi(address, length);
        }

        public static void Write<T>(IntPtr address, T data) where T : struct
        {
            Marshal.StructureToPtr(data, address, true);
        }

        public static void Write<T>(IntPtr address, Int32 offset, T data) where T : struct
        {
            Marshal.StructureToPtr(data, address + offset, true);
        }

        public static void WriteString(IntPtr address, String data)
        {
            Marshal.Copy(Encoding.ASCII.GetBytes(data), 0, address, Encoding.ASCII.GetByteCount(data));
            Marshal.WriteByte(address + Encoding.ASCII.GetByteCount(data), 0x00); // null terminate
        }

        public static T InstallHook<T>(IntPtr address, T newFunc, Boolean inclusive, Boolean exclusive) where T : class
        {
            if (!typeof(Delegate).IsAssignableFrom(typeof(T)))
                throw new InvalidOperationException("Generic T is not a delegate type");

            var oldFunc = Marshal.GetDelegateForFunctionPointer(address, typeof(T));

            Trace.Write($"{(newFunc as Delegate).Method.Name}: 0x{address.ToString("X8")} . ");
            var hook = LocalHook.Create(address, (newFunc as Delegate), null);
            Trace.WriteLine("hook installed!");
            if (inclusive)
                hook.ThreadACL.SetInclusiveACL(new[] { 0 });
            if (exclusive)
                hook.ThreadACL.SetExclusiveACL(new[] { 0 });


            // Unreferences hooks gets cleaned up, so we prevent that by keeping them referenced.
            hooks.Add(hook);

            return (T)(Object)oldFunc;
        }

        public static String PtrAsString(IntPtr address)
        {
            return Memory.ReadString(address);
        }

        public static IntPtr StringAsPtr(String data)
        {
            return Marshal.StringToHGlobalAnsi(data);
        }

        public static IntPtr Alloc(Int32 size)
        {
            return Memory.Alloc(size, MemoryProtection.ReadWrite);
        }

        public static IntPtr Alloc(Int32 size, MemoryProtection protection)
        {
            return Memory.Alloc(size, protection, AllocationType.Commit);
        }

        public static IntPtr Alloc(Int32 size, MemoryProtection protection, AllocationType allocationType)
        {
            return Memory.Alloc(IntPtr.Zero, size, protection, allocationType);
        }

        public static IntPtr Alloc(IntPtr address, Int32 size)
        {
            return Memory.Alloc(address, size, MemoryProtection.ReadWrite);
        }

        public static IntPtr Alloc(IntPtr address, Int32 size, MemoryProtection protection)
        {
            return Memory.Alloc(address, size, protection, AllocationType.Commit);
        }

        public static IntPtr Alloc(IntPtr address, Int32 size, MemoryProtection protection, AllocationType allocationType)
        {
            return Kernel32.VirtualAlloc(address, size, allocationType, protection);
        }

        public static void Free(IntPtr address)
        {
            Kernel32.VirtualFree(address, 0, MemoryFreeType.Release);
        }

        public static void Copy(IntPtr source, IntPtr destination, Int32 size)
        {
            Kernel32.CopyMemory(destination, source, size);
        }
    }
}
