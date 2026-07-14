using IndustrialMonitor.Models.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IndustrialMonitor.ViewModels.DialogWin
{
    /// <summary>
    /// 编辑纵轴
    /// </summary>
    public class TrendAxisEditWinViewModel
    {
        /// <summary>
        /// 趋势
        /// </summary>
        public TrendModel Trend { get; set; }

        /// <summary>
        /// 颜色
        /// </summary>
        public List<string> BrushList { get; set; }
    }
}
