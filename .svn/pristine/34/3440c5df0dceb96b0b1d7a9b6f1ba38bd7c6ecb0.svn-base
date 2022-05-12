using checkmod.TreeGrid;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace checkmod
{
    public class FlatModelHelper
    {
        public static void UpdatePS(ObservableCollection<TypeParam> pars, ref TreeGridModel PS)
        {
            DataGridElement par = null;
            foreach (TypeParam p in pars)
            {
                if (p.children == null) 
                { 
                    par = new DataGridElement(p, false);
                }
                else
                {
                    par = new DataGridElement(p, true);
                    AddSubElements(p.children, par);
                }
                PS.Add(par);
            }
        }

        private static void AddSubElements(ObservableCollection<TypeParam> p, DataGridElement par)
        {
            DataGridElement par1 = null;
            foreach (TypeParam el in p)
            {
                if ((el.children == null) || (el.children.Count == 0))
                {
                    par1 = new DataGridElement(el, false);
                }
                else
                {
                    par1 = new DataGridElement(el, true);
                    AddSubElements(el.children, par1);
                }
                par.Children.Add(par1);
            }
        }
    }
}