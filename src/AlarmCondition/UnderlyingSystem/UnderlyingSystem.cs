/* ========================================================================
 * Copyright (c) 2005-2019 The OPC Foundation, Inc. All rights reserved.
 *
 * OPC Foundation MIT License 1.00
 *
 * Permission is hereby granted, free of charge, to any person
 * obtaining a copy of this software and associated documentation
 * files (the "Software"), to deal in the Software without
 * restriction, including without limitation the rights to use,
 * copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the
 * Software is furnished to do so, subject to the following
 * conditions:
 *
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
 * OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 * NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
 * HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
 * WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
 * FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
 * OTHER DEALINGS IN THE SOFTWARE.
 *
 * The complete license agreement can be found here:
 * http://opcfoundation.org/License/MIT/1.00/
 * ======================================================================*/

using Opc.Ua;
using System;
using System.Collections.Generic;
using System.Threading;

namespace AlarmCondition
{
    /// <summary>
    /// An object that provides access to the underlying system.
    /// </summary>
    public class UnderlyingSystem : IDisposable
    {
        #region Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="UnderlyingSystem"/> class.
        /// </summary>
        public UnderlyingSystem()
        {
            m_sources = new Dictionary<string, UnderlyingSystemSource>();
        }
        #endregion

        #region IDisposable Members
        /// <summary>
        /// The finalizer implementation.
        /// </summary>
        ~UnderlyingSystem()
        {
            Dispose(false);
        }

        /// <summary>
        /// Frees any unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// An overrideable version of the Dispose.
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (m_simulationTimer != null)
                {
                    m_simulationTimer.Dispose();
                    m_simulationTimer = null;
                }
            }
        }
        #endregion

        #region Public Members
        /// <summary>
        /// Creates a source.
        /// </summary>
        /// <param name="sourcePath">The source path.</param>
        /// <param name="alarmChangeCallback">The callback invoked when an alarm changes.</param>
        /// <returns>The source.</returns>
        public UnderlyingSystemSource CreateSource(string sourcePath, AlarmChangedEventHandler alarmChangeCallback)
        {
            UnderlyingSystemSource source = null;

            lock (m_lock)
            {
                // create a new source.
                source = new UnderlyingSystemSource();

                // extract the name from the path.
                string name = sourcePath;

                int index = name.LastIndexOf('/');

                if (index != -1)
                {
                    name = name.Substring(index + 1);
                }

                // extract the type from the path.
                string type = sourcePath;

                index = type.IndexOf('/');

                if (index != -1)
                {
                    type = type.Substring(0, index);
                }

                // create the source.
                source.SourcePath = sourcePath;
                source.Name = name;
                source.SourceType = type;
                source.OnAlarmChanged = alarmChangeCallback;

                m_sources.Add(sourcePath, source);
            }


            // add the alarms based on the source type.
            // note that the source and alarm types used here are types defined by the underlying system.
            // the node manager will need to map these types to UA defined types.
            switch (source.SourceType)
            {
                case "Colours":
                    {
                        source.CreateAlarm("Red", "HighAlarm");
                        source.CreateAlarm("Yellow", "HighLowAlarm");
                        source.CreateAlarm("Green", "TripAlarm");
                        break;
                    }

                case "Metals":
                    {
                        source.CreateAlarm("Gold", "HighAlarm");
                        source.CreateAlarm("Silver", "HighLowAlarm");
                        source.CreateAlarm("Bronze", "TripAlarm");
                        break;
                    }
            }

            // return the new source.
            return source;
        }

        /// <summary>
        /// Starts a simulation which causes the alarm states to change.
        /// </summary>
        /// <remarks>
        /// This simulation randomly activates the alarms that belong to the sources.
        /// Once an alarm is active it has to be acknowledged and confirmed.
        /// Once an alarm is confirmed it go to the inactive state.
        /// If the alarm stays active the severity will be gradually increased.
        /// </remarks>
        public void StartSimulation(bool deterministic = false, int maxIntervalSeconds = 5)
        {
            lock (m_lock)
            {
                if (m_simulationTimer != null)
                {
                    return;
                }

                m_simulationCounter = 0;
                m_nextSourceIndex = 0;

                m_sourceList = new List<UnderlyingSystemSource>(m_sources.Values);
                m_sourceList.Sort(static (a, b) => string.CompareOrdinal(a.SourcePath, b.SourcePath));

                int periodMs;
                if (deterministic && m_sourceList.Count > 0)
                {
                    // Original calculation ensured one visit per source within max interval.
                    periodMs = (int)Math.Floor((maxIntervalSeconds * 1000.0) / m_sourceList.Count);
                    if (periodMs < 50) periodMs = 50;
                }
                else
                {
                    periodMs = 1000;
                }

                if (deterministic && m_sourceList.Count > 0)
                {
                    // Minimum required to guarantee each source within the window
                    int minPerTick = (int)Math.Ceiling(m_sourceList.Count * (double)periodMs / (maxIntervalSeconds * 1000.0));

                    if (minPerTick < 1) minPerTick = 1;

                    // Apply multiplier to increase likelihood of visible alarm transitions
                    m_sourcesPerTick = Math.Min(m_sourceList.Count, minPerTick * m_processingMultiplier);
                }
                else
                {
                    m_sourcesPerTick = m_sourceList.Count; // random mode: process all
                }

                m_simulationTimer = new Timer(
                    deterministic ? DoDeterministicSimulation : DoSimulation,
                    state: null,
                    dueTime: periodMs,
                    period: periodMs);
            }
        }

        /// <summary>
        /// Stops the simulation.
        /// </summary>
        public void StopSimulation()
        {
            lock (m_lock)
            {
                if (m_simulationTimer != null)
                {
                    m_simulationTimer.Dispose();
                    m_simulationTimer = null;
                }
            }
        }

        /// <summary>
        /// Tries the get source.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="source">The source.</param>
        /// <returns>Whether the source was found.</returns>
        public bool TryGetSource(string name, out UnderlyingSystemSource source)
        {
            lock (m_lock)
            {
                foreach (var kvp in m_sources)
                {
                    if (kvp.Value.Name == name)
                    {
                        source = kvp.Value;
                        return true;
                    }
                }
            }
            source = null;
            return false;
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Simulates a source by updating the state of the alarms belonging to the condition.
        /// </summary>
        private void DoSimulation(object state)
        {
            try
            {
                // get the list of sources.
                List<UnderlyingSystemSource> sources = null;

                lock (m_lock)
                {
                    m_simulationCounter++;
                    sources = new List<UnderlyingSystemSource>(m_sources.Values);
                }

                // run simulation for each source.
                for (int ii = 0; ii < sources.Count; ii++)
                {
                    sources[ii].DoSimulation(m_simulationCounter, ii);
                }
            }
            catch (OutOfMemoryException oome)
            {
                Utils.Trace(oome, $"OutOfMemoryException: {oome.Message}");
                Environment.Exit(-1); // Exit app as we cannot recover from this.
            }
            catch (Exception e)
            {
                Utils.Trace(e, "Unexpected error running simulation for system");
            }
        }

        private void DoDeterministicSimulation(object state)
        {
            try
            {
                UnderlyingSystemSource[] sourcesSnapshot;
                int sourcesPerTick;
                long counterStart;

                lock (m_lock)
                {
                    if (m_sourceList == null || m_sourceList.Count == 0)
                    {
                        return;
                    }

                    sourcesSnapshot = m_sourceList.ToArray();
                    sourcesPerTick = m_sourcesPerTick;
                    counterStart = ++m_simulationCounter;
                }

                // Process the batch outside the lock to reduce contention
                for (int i = 0; i < sourcesPerTick; i++)
                {
                    UnderlyingSystemSource source;
                    long counter;

                    lock (m_lock)
                    {
                        if (sourcesSnapshot.Length == 0)
                        {
                            return;
                        }

                        int index = m_nextSourceIndex % sourcesSnapshot.Length;
                        source = sourcesSnapshot[index];
                        m_nextSourceIndex++;

                        // Use a monotonically increasing counter per processed source for better progression
                        counter = counterStart + i;
                    }

                    // Existing per-source simulation method
                    // (Assuming UpdateAlarm/DoSimulation logic inside UnderlyingSystemSource uses counter + index)
                    source.DoSimulation(counter, i);
                }
            }
            catch (Exception ex)
            {
                Utils.Trace(ex, "Deterministic simulation error");
            }
        }
        #endregion

        #region Private Fields
        private readonly object m_lock = new object();
        private readonly Dictionary<string, UnderlyingSystemSource> m_sources;
        private Timer m_simulationTimer;
        private long m_simulationCounter;
        private int m_sourcesPerTick;
        private readonly int m_processingMultiplier = 2; // Increase density; make configurable if desired.
        private int m_nextSourceIndex;
        private List<UnderlyingSystemSource> m_sourceList; // cached ordered list
        #endregion
    }
}
