using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace LM
{
    public class SimpleTimer
    {
        private readonly float _interval;
        private float _elapsedTime;
        private float _targetTime;

        public TimerState State { get; private set; }

        public SimpleTimer(float interval, bool startReady = true)
        {
            _interval = interval;
            State = startReady ? TimerState.Ready : TimerState.Off;
            _elapsedTime = 0f;
            _targetTime = interval;
        }

        public event Action OnReady;
        public event Action OnNotReady;

        /// <summary>
        /// Starts an asynchronous wait.
        /// </summary>
        public async UniTask WaitAsync(float? seconds = null, bool ignoreTimeScale = false)
        {
            if (State is TimerState.Waiting or TimerState.ManualWaiting) return;

            OnNotReady?.Invoke();
            _targetTime = seconds ?? _interval;
            State = TimerState.Waiting;

            try
            {
                await UniTask.Delay(TimeSpan.FromSeconds(_targetTime), ignoreTimeScale: ignoreTimeScale, delayTiming: PlayerLoopTiming.Update, cancellationToken: System.Threading.CancellationToken.None);

                if (State == TimerState.Waiting)
                {
                    State = TimerState.Ready;
                    OnReady?.Invoke();
                }
                else
                {
                    Debug.LogWarning("Timer not ready because it was cancelled.");
                }
            }
            catch (OperationCanceledException)
            {
                if (State == TimerState.Waiting)
                {
                     State = TimerState.Off;
                }
            }
        }

        /// <summary>
        /// Starts a manual wait process, requires calling Tick().
        /// </summary>
        public void StartManualWait(float? seconds = null)
        {
             if (State is TimerState.Waiting or TimerState.ManualWaiting) return;

            OnNotReady?.Invoke();
            _targetTime = seconds ?? _interval;
            _elapsedTime = 0f;
            State = TimerState.ManualWaiting;
        }

        /// <summary>
        /// Advances the timer manually if it's in the ManualWaiting state.
        /// </summary>
        /// <param name="deltaTime">Time elapsed since the last frame (e.g., Time.deltaTime).</param>
        public void Tick(float deltaTime)
        {
            if (State != TimerState.ManualWaiting) return;

            _elapsedTime += deltaTime;
            
            if (!(_elapsedTime >= _targetTime)) return;
            
            State = TimerState.Ready;
            OnReady?.Invoke();
        }

        /// <summary>
        /// Checks if the timer is in the Ready state.
        /// Optionally sets the state to Off after checking.
        /// </summary>
        public bool IsFinished(bool setReadyToOff = false)
        {
            var result = State == TimerState.Ready;

            if (result && setReadyToOff)
            {
                State = TimerState.Off;
            }

            return result;
        }

        /// <summary>
        /// Resets the timer to a specific state.
        /// </summary>
        public void Reset(bool ready = true)
        {
             State = ready ? TimerState.Ready : TimerState.Off;
             _elapsedTime = 0f;
        }

        public enum TimerState
        {
            Ready,
            Waiting,
            ManualWaiting,
            Off,
        }
    }
}