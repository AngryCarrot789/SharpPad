//
// Copyright (c) 2023-2024 REghZy
//
// This file is part of SharpPad.
//
// SharpPad is free software; you can redistribute it and/or
// modify it under the terms of the GNU General Public License
// as published by the Free Software Foundation; either
// version 3.0 of the License, or (at your option) any later version.
//
// SharpPad is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU
// Lesser General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with SharpPad. If not, see <https://www.gnu.org/licenses/>.
//

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace SharpPad.WPF
{
    public class AsyncDemo
    {
        // This is the written code
        // public void Run()
        // {
        //     new Thread(async () =>
        //     {
        //         // SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(Current.Dispatcher));
        //         Console.WriteLine("A");
        //         await Task.Delay(100);
        //         Console.WriteLine("B");
        //         await Task.Delay(200);
        //         Console.WriteLine("C");
        //         await Task.Delay(300);
        //         Console.WriteLine("D");
        //         await Task.Delay(400);
        //         Console.WriteLine("E");
        //         await Task.Delay(500);
        //         Console.WriteLine("F");
        //     }).Start();
        // }

        private sealed class SomeClass
        {
            public static readonly SomeClass Instance = new SomeClass();

            [StructLayout(LayoutKind.Auto)]
            private struct TheStateMachine : IAsyncStateMachine
            {
                public int myState;
                public AsyncVoidMethodBuilder myBuilder;
                private TaskAwaiter myAwaiter;

                private void MoveNext()
                {
                    int state = this.myState;
                    try
                    {
                        TaskAwaiter awaiter;
                        switch (state)
                        {
                            default:
                                Console.WriteLine("A");
                                awaiter = Task.Delay(100).GetAwaiter();
                                if (!awaiter.IsCompleted)
                                {
                                    state = (this.myState = 0);
                                    this.myAwaiter = awaiter;
                                    this.myBuilder.AwaitUnsafeOnCompleted(ref awaiter, ref this);
                                    return;
                                }

                                goto IL_0080;
                            case 0:
                                awaiter = this.myAwaiter;
                                this.myAwaiter = default;
                                state = (this.myState = -1);
                                goto IL_0080;
                            case 1:
                                awaiter = this.myAwaiter;
                                this.myAwaiter = default;
                                state = (this.myState = -1);
                                goto IL_00e9;
                            case 2:
                                awaiter = this.myAwaiter;
                                this.myAwaiter = default;
                                state = (this.myState = -1);
                                goto IL_0152;
                            case 3:
                                awaiter = this.myAwaiter;
                                this.myAwaiter = default;
                                state = (this.myState = -1);
                                goto IL_01bb;
                            case 4:
                            {
                                awaiter = this.myAwaiter;
                                this.myAwaiter = default;
                                state = (this.myState = -1);
                                break;
                            }
                                IL_01bb:
                                awaiter.GetResult();
                                Console.WriteLine("E");
                                awaiter = Task.Delay(500).GetAwaiter();
                                if (!awaiter.IsCompleted)
                                {
                                    state = (this.myState = 4);
                                    this.myAwaiter = awaiter;
                                    this.myBuilder.AwaitUnsafeOnCompleted(ref awaiter, ref this);
                                    return;
                                }

                                break;
                                IL_0080:
                                awaiter.GetResult();
                                Console.WriteLine("B");
                                awaiter = Task.Delay(200).GetAwaiter();
                                if (!awaiter.IsCompleted)
                                {
                                    state = (this.myState = 1);
                                    this.myAwaiter = awaiter;
                                    this.myBuilder.AwaitUnsafeOnCompleted(ref awaiter, ref this);
                                    return;
                                }

                                goto IL_00e9;
                                IL_0152:
                                awaiter.GetResult();
                                Console.WriteLine("D");
                                awaiter = Task.Delay(400).GetAwaiter();
                                if (!awaiter.IsCompleted)
                                {
                                    state = (this.myState = 3);
                                    this.myAwaiter = awaiter;
                                    this.myBuilder.AwaitUnsafeOnCompleted(ref awaiter, ref this);
                                    return;
                                }

                                goto IL_01bb;
                                IL_00e9:
                                awaiter.GetResult();
                                Console.WriteLine("C");
                                awaiter = Task.Delay(300).GetAwaiter();
                                if (!awaiter.IsCompleted)
                                {
                                    state = (this.myState = 2);
                                    this.myAwaiter = awaiter;
                                    this.myBuilder.AwaitUnsafeOnCompleted(ref awaiter, ref this);
                                    return;
                                }

                                goto IL_0152;
                        }

                        awaiter.GetResult();
                        Console.WriteLine("F");
                    }
                    catch (Exception exception)
                    {
                        this.myState = -2;
                        this.myBuilder.SetException(exception);
                        return;
                    }

                    this.myState = -2;
                    this.myBuilder.SetResult();
                }

                void IAsyncStateMachine.MoveNext()
                {
                    this.MoveNext();
                }

                [DebuggerHidden]
                private void SetStateMachine(IAsyncStateMachine stateMachine)
                {
                    this.myBuilder.SetStateMachine(stateMachine);
                }

                void IAsyncStateMachine.SetStateMachine(IAsyncStateMachine stateMachine) => this.SetStateMachine(stateMachine);
            }

            public static ThreadStart ThreadStateArgs;

            [AsyncStateMachine(typeof(TheStateMachine))]
            internal void Run()
            {
                TheStateMachine machine = default;
                machine.myBuilder = AsyncVoidMethodBuilder.Create();
                machine.myState = -1;
                machine.myBuilder.Start(ref machine);
            }
        }

        public void Run()
        {
            new Thread(SomeClass.ThreadStateArgs ?? (SomeClass.ThreadStateArgs = new ThreadStart(SomeClass.Instance.Run))).Start();
        }
    }
}