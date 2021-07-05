﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace Squared.Util {
    [TestFixture]
    public class SortTests {
        public struct BitmapDrawCall {
            public int TextureID;
            public float SortKey;

            public override string ToString () {
                return String.Format("{{ TextureID = {0}, SortKey = {1} }}", TextureID, SortKey);
            }
        }

        public class BitmapDrawCallComparer : IComparer<BitmapDrawCall> {            
            public int Compare(BitmapDrawCall x, BitmapDrawCall y) {
                var result = (x.SortKey > y.SortKey)
                    ? 1
                    : (
                        (x.SortKey < y.SortKey)
                        ? -1
                        : 0
                    );
                if (result == 0)
                    result = (x.TextureID > y.TextureID)
                    ? 1
                    : (
                        (x.TextureID < y.TextureID)
                        ? -1
                        : 0
                    );
                return result;
            }
        }

        public class IntComparer : IComparer<int> {
            public int Compare (int x, int y) {
                return x - y;
            }
        }

        public readonly int[] SortedSequence, ReversedSequence, SmallRandomSequence, LargeRandomSequence;

        public SortTests () {
            SortedSequence = new[] { 1, 2, 4, 8, 16, 32, 64, 128, 129, 130, 140, 150, 160, 1234567 };
            ReversedSequence = SortedSequence.Reverse().ToArray();

            SmallRandomSequence = MakeRandomSequence(4096, 123456);
            LargeRandomSequence = MakeRandomSequence(1024 * 1024 * 8, 234567);
        }

        private int[] MakeRandomSequence (int count, int seed) {
            // Fixed seed for reproducability
            var rng = new System.Random(seed);
            var result = new int[count];

            for (var i = 0; i < count; i++)
                result[i] = rng.Next();

            return result;
        }

        private void AssertSortsCorrectly<T, TComparer> (T[] theSequence, TComparer comparer)
            where TComparer: IComparer<T>
        {
            var BCLCopy = (T[])theSequence.Clone();
            var SorterCopy = (T[])theSequence.Clone();

            var stopwatch = new Stopwatch();

            stopwatch.Reset();
            stopwatch.Start();
            Array.Sort(BCLCopy, comparer);
            stopwatch.Stop();

            var BCLElapsed = stopwatch.Elapsed.TotalMilliseconds;

            stopwatch.Reset();
            stopwatch.Start();
            Sort.FastCLRSort(SorterCopy, comparer);
            stopwatch.Stop();

            var TimsortElapsed = stopwatch.Elapsed.TotalMilliseconds;

            if (BCLCopy.Length > 1) {
                for (var i = 1; i < BCLCopy.Length; i++) {
                    if (comparer.Compare(SorterCopy[i - 1], SorterCopy[i]) > 0) {
                        Assert.Fail(
                            "Timsorted sequence is unsorted at index {0}.\r\n{1}: {2}\r\n{3}: {4}",
                            i, i - 1,
                            SorterCopy[i - 1], i, SorterCopy[i]
                        );
                    }
                }
            }

            Console.WriteLine("Elapsed: BCL = {0:000.00}ms, Timsort = {1:000.00}ms", BCLElapsed, TimsortElapsed);
        }

        [Test]
        public void SortsNoItems () {
            AssertSortsCorrectly(new int[] {}, new IntComparer());
        }

        [Test]
        public void SortsOneItem () {
            AssertSortsCorrectly(new int[] { 1 }, new IntComparer());
        }

        [Test]
        public void SortsSortedSequence () {
            AssertSortsCorrectly(SortedSequence, new IntComparer());
        }

        [Test]
        public void SortsReversedSequence () {
            AssertSortsCorrectly(ReversedSequence, new IntComparer());
        }

        [Test]
        public void SortsSmallRandomSequence () {
            AssertSortsCorrectly(SmallRandomSequence, new IntComparer());
        }

        [Test]
        public void SortsLargeRandomSequence () {
            AssertSortsCorrectly(LargeRandomSequence, new IntComparer());
        }

        [Test]
        public void SortsParticularDrawCallSequence () {
            var drawCalls = new [] {
                new BitmapDrawCall { TextureID=66824994, SortKey=1 },
                new BitmapDrawCall { TextureID=66824994, SortKey=1 },
                new BitmapDrawCall { TextureID=66824994, SortKey=1 },
                new BitmapDrawCall { TextureID=66824994, SortKey=1 },
                new BitmapDrawCall { TextureID=66824994, SortKey=1 },
                new BitmapDrawCall { TextureID=66824994, SortKey=1 },
                new BitmapDrawCall { TextureID=66824994, SortKey=1 },
                new BitmapDrawCall { TextureID=66824994, SortKey=1 },
                new BitmapDrawCall { TextureID=66824994, SortKey=1 },
                new BitmapDrawCall { TextureID=66824994, SortKey=1 },
                new BitmapDrawCall { TextureID=66824994, SortKey=1 },
                new BitmapDrawCall { TextureID=66824994, SortKey=1 },
                new BitmapDrawCall { TextureID=66824994, SortKey=0 },
                new BitmapDrawCall { TextureID=66824994, SortKey=0 },
                new BitmapDrawCall { TextureID=66824994, SortKey=0 },
                new BitmapDrawCall { TextureID=66824994, SortKey=0 },
                new BitmapDrawCall { TextureID=66824994, SortKey=0 },
                new BitmapDrawCall { TextureID=66824994, SortKey=0 },
                new BitmapDrawCall { TextureID=66824994, SortKey=0 },
                new BitmapDrawCall { TextureID=66824994, SortKey=0 },
                new BitmapDrawCall { TextureID=66824994, SortKey=0 },
                new BitmapDrawCall { TextureID=66824994, SortKey=0 },
                new BitmapDrawCall { TextureID=66824994, SortKey=0 },
                new BitmapDrawCall { TextureID=66824994, SortKey=0 },
                new BitmapDrawCall { TextureID=66824994, SortKey=0 },
                new BitmapDrawCall { TextureID=66824994, SortKey=0 },
                new BitmapDrawCall { TextureID=66824994, SortKey=0 },
                new BitmapDrawCall { TextureID=66824994, SortKey=0 },
                new BitmapDrawCall { TextureID=66824994, SortKey=0 },
                new BitmapDrawCall { TextureID=66824994, SortKey=0 },
                new BitmapDrawCall { TextureID=66824994, SortKey=0 },
                new BitmapDrawCall { TextureID=66824994, SortKey=0 },
                new BitmapDrawCall { TextureID=66824994, SortKey=0 },
                new BitmapDrawCall { TextureID=66824994, SortKey=0 },
                new BitmapDrawCall { TextureID=66824994, SortKey=0 },
                new BitmapDrawCall { TextureID=66824994, SortKey=0 },
                new BitmapDrawCall { TextureID=66824994, SortKey=0 },
                new BitmapDrawCall { TextureID=66824994, SortKey=0 },
                new BitmapDrawCall { TextureID=66824994, SortKey=0 },
                new BitmapDrawCall { TextureID=66824994, SortKey=0 },
                new BitmapDrawCall { TextureID=66824994, SortKey=0 },
                new BitmapDrawCall { TextureID=66824994, SortKey=0 },
                new BitmapDrawCall { TextureID=66824994, SortKey=0 },
                new BitmapDrawCall { TextureID=66824994, SortKey=0 },
                new BitmapDrawCall { TextureID=66824994, SortKey=0 },
                new BitmapDrawCall { TextureID=66824994, SortKey=0 },
                new BitmapDrawCall { TextureID=66824994, SortKey=0 },
                new BitmapDrawCall { TextureID=66824994, SortKey=0 },
                new BitmapDrawCall { TextureID=66824994, SortKey=1 },
                new BitmapDrawCall { TextureID=66824994, SortKey=1 },
                new BitmapDrawCall { TextureID=66824994, SortKey=1 },
                new BitmapDrawCall { TextureID=66824994, SortKey=1 },
                new BitmapDrawCall { TextureID=66824994, SortKey=1 },
                new BitmapDrawCall { TextureID=66824994, SortKey=1 },
                new BitmapDrawCall { TextureID=66824994, SortKey=1 },
                new BitmapDrawCall { TextureID=66824994, SortKey=1 },
                new BitmapDrawCall { TextureID=66824994, SortKey=1 },
                new BitmapDrawCall { TextureID=66824994, SortKey=1 },
                new BitmapDrawCall { TextureID=66824994, SortKey=1 },
                new BitmapDrawCall { TextureID=66824994, SortKey=1 },
                new BitmapDrawCall { TextureID=66824994, SortKey=1 },
                new BitmapDrawCall { TextureID=66824994, SortKey=1 },
                new BitmapDrawCall { TextureID=66824994, SortKey=1 },
                new BitmapDrawCall { TextureID=66824994, SortKey=1 },
                new BitmapDrawCall { TextureID=66824994, SortKey=1 },
                new BitmapDrawCall { TextureID=66824994, SortKey=1 },
                new BitmapDrawCall { TextureID=66824994, SortKey=1 },
                new BitmapDrawCall { TextureID=66824994, SortKey=1 },
                new BitmapDrawCall { TextureID=66824994, SortKey=0 },
                new BitmapDrawCall { TextureID=66824994, SortKey=0 },
                new BitmapDrawCall { TextureID=66824994, SortKey=0 },
                new BitmapDrawCall { TextureID=66824994, SortKey=0 },
                new BitmapDrawCall { TextureID=66824994, SortKey=0 },
                new BitmapDrawCall { TextureID=66824994, SortKey=0 },
                new BitmapDrawCall { TextureID=66824994, SortKey=0 },
                new BitmapDrawCall { TextureID=66824994, SortKey=0 },
                new BitmapDrawCall { TextureID=66824994, SortKey=0 },
                new BitmapDrawCall { TextureID=66824994, SortKey=0 },
                new BitmapDrawCall { TextureID=66824994, SortKey=0 },
                new BitmapDrawCall { TextureID=66824994, SortKey=0 },
                new BitmapDrawCall { TextureID=66824994, SortKey=0 },
                new BitmapDrawCall { TextureID=66824994, SortKey=0 },
                new BitmapDrawCall { TextureID=66824994, SortKey=0 },
                new BitmapDrawCall { TextureID=66824994, SortKey=0 },
                new BitmapDrawCall { TextureID=66824994, SortKey=0 },
                new BitmapDrawCall { TextureID=66824994, SortKey=0 },
                new BitmapDrawCall { TextureID=66824994, SortKey=0 },
                new BitmapDrawCall { TextureID=66824994, SortKey=0 },
                new BitmapDrawCall { TextureID=66824994, SortKey=0 },
                new BitmapDrawCall { TextureID=66824994, SortKey=0 },
                new BitmapDrawCall { TextureID=66824994, SortKey=0 },
                new BitmapDrawCall { TextureID=66824994, SortKey=0 },
                new BitmapDrawCall { TextureID=66824994, SortKey=0 },
                new BitmapDrawCall { TextureID=66824994, SortKey=0 },
                new BitmapDrawCall { TextureID=66824994, SortKey=0 },
                new BitmapDrawCall { TextureID=66824994, SortKey=0 },
                new BitmapDrawCall { TextureID=66824994, SortKey=0 },
                new BitmapDrawCall { TextureID=66824994, SortKey=0 },
                new BitmapDrawCall { TextureID=66824994, SortKey=0 },
                new BitmapDrawCall { TextureID=66824994, SortKey=0 },
                new BitmapDrawCall { TextureID=66824994, SortKey=0 },
                new BitmapDrawCall { TextureID=66824994, SortKey=0 },
                new BitmapDrawCall { TextureID=66824994, SortKey=0 },
                new BitmapDrawCall { TextureID=66824994, SortKey=0 },
                new BitmapDrawCall { TextureID=66824994, SortKey=0 },
                new BitmapDrawCall { TextureID=66824994, SortKey=0 },
                new BitmapDrawCall { TextureID=66824994, SortKey=0 },
                new BitmapDrawCall { TextureID=66824994, SortKey=0 },
                new BitmapDrawCall { TextureID=66824994, SortKey=0 },
                new BitmapDrawCall { TextureID=66824994, SortKey=0 },
                new BitmapDrawCall { TextureID=66824994, SortKey=0 },
                new BitmapDrawCall { TextureID=66824994, SortKey=0 },
                new BitmapDrawCall { TextureID=66824994, SortKey=0 },
                new BitmapDrawCall { TextureID=66824994, SortKey=0 },
                new BitmapDrawCall { TextureID=66824994, SortKey=0 },
                new BitmapDrawCall { TextureID=66824994, SortKey=0 },
                new BitmapDrawCall { TextureID=66824994, SortKey=0 },
                new BitmapDrawCall { TextureID=66824994, SortKey=0 },
                new BitmapDrawCall { TextureID=66824994, SortKey=0 },
                new BitmapDrawCall { TextureID=66824994, SortKey=0 },
                new BitmapDrawCall { TextureID=66824994, SortKey=0 },
                new BitmapDrawCall { TextureID=66824994, SortKey=0 },
                new BitmapDrawCall { TextureID=66824994, SortKey=0 },
                new BitmapDrawCall { TextureID=66824994, SortKey=0 },
                new BitmapDrawCall { TextureID=66824994, SortKey=0 },
                new BitmapDrawCall { TextureID=66824994, SortKey=0 },
                new BitmapDrawCall { TextureID=66824994, SortKey=0 },
                new BitmapDrawCall { TextureID=66824994, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 },
                new BitmapDrawCall { TextureID=62468121, SortKey=0 }
            };

            AssertSortsCorrectly(drawCalls, new BitmapDrawCallComparer());
        }

        [Test]
        public void SortsParticularDrawCallSequence2 () {
            var drawCalls = new[] {
                new BitmapDrawCall { TextureID = -1693165824, SortKey = 0 },
                new BitmapDrawCall { TextureID = -1693165824, SortKey = 0 },
                new BitmapDrawCall { TextureID = -1693165824, SortKey = 0 },
                new BitmapDrawCall { TextureID = -1693165824, SortKey = 0 },
                new BitmapDrawCall { TextureID = 817334784, SortKey = 0 },
                new BitmapDrawCall { TextureID = 817334784, SortKey = 0 },
                new BitmapDrawCall { TextureID = 817334784, SortKey = 0 },
                new BitmapDrawCall { TextureID = 817334784, SortKey = 0 },
                new BitmapDrawCall { TextureID = 817334784, SortKey = 0 },
                new BitmapDrawCall { TextureID = 817334784, SortKey = 0 },
                new BitmapDrawCall { TextureID = 817334784, SortKey = 0 },
                new BitmapDrawCall { TextureID = 817334784, SortKey = 0 },
                new BitmapDrawCall { TextureID = 817334784, SortKey = 0 },
                new BitmapDrawCall { TextureID = 817334784, SortKey = 0 },
                new BitmapDrawCall { TextureID = 817334784, SortKey = 0 },
                new BitmapDrawCall { TextureID = -1693165824, SortKey = 0 },
                new BitmapDrawCall { TextureID = -1693165824, SortKey = 0 },
                new BitmapDrawCall { TextureID = -1693165824, SortKey = 0 },
                new BitmapDrawCall { TextureID = -1693165824, SortKey = 0 },
                new BitmapDrawCall { TextureID = -1693165824, SortKey = 0 },
                new BitmapDrawCall { TextureID = -1693165824, SortKey = 0 },
                new BitmapDrawCall { TextureID = -1693165824, SortKey = 0 },
                new BitmapDrawCall { TextureID = -1693165824, SortKey = 0 },
                new BitmapDrawCall { TextureID = 817334784, SortKey = 0 },
                new BitmapDrawCall { TextureID = 817334784, SortKey = 0 },
                new BitmapDrawCall { TextureID = 817334784, SortKey = 0 },
                new BitmapDrawCall { TextureID = 817334784, SortKey = 0 },
                new BitmapDrawCall { TextureID = -1693165824, SortKey = 0 },
                new BitmapDrawCall { TextureID = -1693165824, SortKey = 0 },
                new BitmapDrawCall { TextureID = -1693165824, SortKey = 0 },
                new BitmapDrawCall { TextureID = -1693165824, SortKey = 0 },
                new BitmapDrawCall { TextureID = -1693165824, SortKey = 0 },
                new BitmapDrawCall { TextureID = -1112338688, SortKey = 0 },
                new BitmapDrawCall { TextureID = -1112338688, SortKey = 0 },
                new BitmapDrawCall { TextureID = 817334784, SortKey = 0 },
                new BitmapDrawCall { TextureID = 817334784, SortKey = 0 },
                new BitmapDrawCall { TextureID = -1112338688, SortKey = 0 },
                new BitmapDrawCall { TextureID = -1112338688, SortKey = 0 },
                new BitmapDrawCall { TextureID = -1693165824, SortKey = 0 },
                new BitmapDrawCall { TextureID = -1693165824, SortKey = 0 },
                new BitmapDrawCall { TextureID = -1693165824, SortKey = 0 },
                new BitmapDrawCall { TextureID = -1693165824, SortKey = 0 },
                new BitmapDrawCall { TextureID = -1693165824, SortKey = 0 },
                new BitmapDrawCall { TextureID = 817334784, SortKey = 0 },
                new BitmapDrawCall { TextureID = 817334784, SortKey = 0 },
                new BitmapDrawCall { TextureID = 817334784, SortKey = 0 },
                new BitmapDrawCall { TextureID = 817334784, SortKey = 0 },
                new BitmapDrawCall { TextureID = 817334784, SortKey = 0 },
                new BitmapDrawCall { TextureID = 817334784, SortKey = 0 },
                new BitmapDrawCall { TextureID = 817334784, SortKey = 0 },
                new BitmapDrawCall { TextureID = -1693165824, SortKey = 0 },
                new BitmapDrawCall { TextureID = -1693165824, SortKey = 0 },
                new BitmapDrawCall { TextureID = -1693165824, SortKey = 0 },
                new BitmapDrawCall { TextureID = -1693165824, SortKey = 0 },
                new BitmapDrawCall { TextureID = -1693165824, SortKey = 0 },
                new BitmapDrawCall { TextureID = -1693165824, SortKey = 0 },
                new BitmapDrawCall { TextureID = 817334784, SortKey = 0 },
                new BitmapDrawCall { TextureID = 817334784, SortKey = 0 },
                new BitmapDrawCall { TextureID = 817334784, SortKey = 0 },
                new BitmapDrawCall { TextureID = 817334784, SortKey = 0 },
                new BitmapDrawCall { TextureID = 817334784, SortKey = 0 },
                new BitmapDrawCall { TextureID = -1693165824, SortKey = 0 },
                new BitmapDrawCall { TextureID = -1693165824, SortKey = 0 },
                new BitmapDrawCall { TextureID = 817334784, SortKey = 0 },
                new BitmapDrawCall { TextureID = 817334784, SortKey = 0 },
                new BitmapDrawCall { TextureID = 817334784, SortKey = 0 },
                new BitmapDrawCall { TextureID = 817334784, SortKey = 0 },
                new BitmapDrawCall { TextureID = -1693165824, SortKey = 0 },
                new BitmapDrawCall { TextureID = -1693165824, SortKey = 0 },
                new BitmapDrawCall { TextureID = 817334784, SortKey = 0 },
                new BitmapDrawCall { TextureID = 817334784, SortKey = 0 },
                new BitmapDrawCall { TextureID = 817334784, SortKey = 0 },
                new BitmapDrawCall { TextureID = 817334784, SortKey = 0 },
                new BitmapDrawCall { TextureID = -1693165824, SortKey = 0 },
                new BitmapDrawCall { TextureID = -1693165824, SortKey = 0 },
                new BitmapDrawCall { TextureID = -1693165824, SortKey = 0 },
                new BitmapDrawCall { TextureID = -1693165824, SortKey = 0 },
                new BitmapDrawCall { TextureID = -1693165824, SortKey = 0 },
                new BitmapDrawCall { TextureID = -1693165824, SortKey = 0 },
                new BitmapDrawCall { TextureID = 817334784, SortKey = 0 },
                new BitmapDrawCall { TextureID = 817334784, SortKey = 0 },
                new BitmapDrawCall { TextureID = -1693165824, SortKey = 0 },
                new BitmapDrawCall { TextureID = -1693165824, SortKey = 0 },
                new BitmapDrawCall { TextureID = -1693165824, SortKey = 0 },
                new BitmapDrawCall { TextureID = -1693165824, SortKey = 0 },
                new BitmapDrawCall { TextureID = -1693165824, SortKey = 0 },
                new BitmapDrawCall { TextureID = -1693165824, SortKey = 0 },
                new BitmapDrawCall { TextureID = -1693165824, SortKey = 0 },
                new BitmapDrawCall { TextureID = -1693165824, SortKey = 0 },
                new BitmapDrawCall { TextureID = 817334784, SortKey = 0 },
                new BitmapDrawCall { TextureID = 817334784, SortKey = 0 },
                new BitmapDrawCall { TextureID = 817334784, SortKey = 0 },
                new BitmapDrawCall { TextureID = 817334784, SortKey = 0 },
                new BitmapDrawCall { TextureID = 817334784, SortKey = 0 },
                new BitmapDrawCall { TextureID = 817334784, SortKey = 0 },
                new BitmapDrawCall { TextureID = -1693165824, SortKey = 0 },
                new BitmapDrawCall { TextureID = -1693165824, SortKey = 0 },
                new BitmapDrawCall { TextureID = -1693165824, SortKey = 0 },
                new BitmapDrawCall { TextureID = -1693165824, SortKey = 0 },
                new BitmapDrawCall { TextureID = -1693165824, SortKey = 0 },
                new BitmapDrawCall { TextureID = -1693165824, SortKey = 0 },
                new BitmapDrawCall { TextureID = -1693165824, SortKey = 0 },
                new BitmapDrawCall { TextureID = -1693165824, SortKey = 0 },
                new BitmapDrawCall { TextureID = 817334784, SortKey = 0 },
                new BitmapDrawCall { TextureID = 817334784, SortKey = 0 },
                new BitmapDrawCall { TextureID = 817334784, SortKey = 0 },
                new BitmapDrawCall { TextureID = 817334784, SortKey = 0 },
                new BitmapDrawCall { TextureID = -1693165824, SortKey = 0 },
                new BitmapDrawCall { TextureID = -1693165824, SortKey = 0 },
                new BitmapDrawCall { TextureID = -1693165824, SortKey = 0 },
                new BitmapDrawCall { TextureID = -1693165824, SortKey = 0 },
                new BitmapDrawCall { TextureID = -1693165824, SortKey = 0 },
                new BitmapDrawCall { TextureID = -1693165824, SortKey = 0 },
                new BitmapDrawCall { TextureID = -1693165824, SortKey = 0 },
                new BitmapDrawCall { TextureID = -1693165824, SortKey = 0 },
                new BitmapDrawCall { TextureID = 817334784, SortKey = 0 },
                new BitmapDrawCall { TextureID = 817334784, SortKey = 0 },
                new BitmapDrawCall { TextureID = 817334784, SortKey = 0 },
                new BitmapDrawCall { TextureID = 817334784, SortKey = 0 },
                new BitmapDrawCall { TextureID = 817334784, SortKey = 0 },
                new BitmapDrawCall { TextureID = 817334784, SortKey = 0 },
                new BitmapDrawCall { TextureID = 817334784, SortKey = 0 },
                new BitmapDrawCall { TextureID = -1693165824, SortKey = 0 },
                new BitmapDrawCall { TextureID = -1693165824, SortKey = 0 },
                new BitmapDrawCall { TextureID = -1693165824, SortKey = 0 },
                new BitmapDrawCall { TextureID = -1693165824, SortKey = 0 },
            };

            AssertSortsCorrectly(drawCalls, new BitmapDrawCallComparer());
        }
    }
}
