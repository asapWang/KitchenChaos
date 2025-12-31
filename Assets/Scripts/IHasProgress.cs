using UnityEngine;
using System;
public interface IHasProgress
{
    public event EventHandler<OnProgressBarUIChangedEventArgs> OnProgressBarUIChanged;
    public class OnProgressBarUIChangedEventArgs : EventArgs
    {
        public float progressNormalized;
    }
}
