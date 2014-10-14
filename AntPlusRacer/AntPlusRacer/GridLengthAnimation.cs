/*
This software is subject to the license described in the license.txt file included with this software distribution.You may not use this file except in compliance with this license. 
Copyright © Dynastream Innovations Inc. 2012
All rights reserved.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media.Animation;
using System.Windows;

namespace AntPlusRacer
{
    class GridLengthAnimation: AnimationTimeline
    {
        //Create the dependency properties and register them
        public static readonly DependencyProperty FromProperty = DependencyProperty.Register(
                                                         "From",
                                                         typeof(double?),
                                                         typeof(GridLengthAnimation),
                                                         new PropertyMetadata(null));
        public static readonly DependencyProperty ToProperty = DependencyProperty.Register(
                                                         "To",
                                                         typeof(double?),
                                                         typeof(GridLengthAnimation),
                                                         new PropertyMetadata(null));

        //Accessors to the dependency properties as nullable doubles
        public double? From
        {
            get { return (double?)this.GetValue(FromProperty); }
            set { this.SetValue(FromProperty, value); }
        }
        public double? To
        {
            get { return (double?)this.GetValue(ToProperty); }
            set { this.SetValue(ToProperty, value); }
        }

        static GridLengthAnimation()
        {
        }

        public override Type TargetPropertyType
        {
            get{ return typeof(GridLength); }
        }

        protected override System.Windows.Freezable CreateInstanceCore()
        {
            return new GridLengthAnimation();
        }

        public override object GetCurrentValue(object defaultOriginValue,
            object defaultDestinationValue, AnimationClock animationClock)
        {
            return new GridLength(From.Value + ((To.Value - From.Value) * animationClock.CurrentProgress.Value), GridUnitType.Star);
        }
    }
}
