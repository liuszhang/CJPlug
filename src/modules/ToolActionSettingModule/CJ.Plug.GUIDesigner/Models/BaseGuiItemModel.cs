using Blazor.Diagrams.Core.Geometry;
using Blazor.Diagrams.Core.Models;
using CJ.Plug.GUIDesigner.Contracts;
using CJ.Plug.GUIDesigner.Services;
using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CJ.Plug.GUIDesigner.Models
{
    public class BaseGuiItemModel:NodeModel
    {
        private readonly IGuiItemHandlerService GuiItemHandlerService;

        public IGuiItemService GuiItemService { get; set; }

        public BaseGuiItemModel(string? GuiName, IGuiItemHandlerService guiItemHandlerService, Point? position = null) : base(position) 
        {
            GuiItemHandlerService= guiItemHandlerService;
            GuiItemService = GuiItemHandlerService.GetGuiHandler(GuiName);
        }
    }
}
