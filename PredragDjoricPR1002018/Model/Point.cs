using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PredragDjoricPR1002018.Model
{
    public class Point
    {
        private double x;
        private double y;

        public Point()
        {

        }

        public Point(double X,double Y) //treba mi za vertices
        {
            x = X;
            y = Y;
        }

        public double X
        {
            get
            {
                return x;
            }

            set
            {
                x = value;
            }
        }

        public double Y
        {
            get
            {
                return y;
            }

            set
            {
                y = value;
            }
        }
    }
}
