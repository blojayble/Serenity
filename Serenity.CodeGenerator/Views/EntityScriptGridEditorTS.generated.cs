﻿#pragma warning disable 1591
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Serenity.CodeGenerator.Views
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("RazorGenerator", "2.0.0.0")]
    public partial class EntityScriptGridEditorTS : RazorGenerator.Templating.RazorTemplateBase
    {
#line hidden
 public dynamic Model { get; set; } 
        public override void Execute()
        {


WriteLiteral("\r\n");



                                                   
    var dotModule = Model.Module == null ? "" : ("." + Model.Module);
    var moduleDot = Model.Module == null ? "" : (Model.Module  + ".");


WriteLiteral("/// <reference path=\"../../Common/Helpers/GridEditorBase.ts\" />\r\n\r\nnamespace ");


      Write(Model.RootNamespace);


                            Write(dotModule);

WriteLiteral(" {\r\n    \r\n    ");


WriteLiteral("@Serenity.Decorators.registerClass()\r\n    export class ");


             Write(Model.ClassName);

WriteLiteral("Editor extends Common.GridEditorBase<");


                                                                    Write(Model.RowClassName);

WriteLiteral("> {\r\n        protected getColumnsKey() { return \'");


                                        Write(moduleDot);


                                                    Write(Model.ClassName);

WriteLiteral("\'; }\r\n        protected getDialogType() { return ");


                                       Write(Model.ClassName);


                                                             WriteLiteral("EditorDialog; }");

                                                                             if (Model.Identity != null) {
WriteLiteral("\r\n        ");


               }

WriteLiteral("        protected getLocalTextPrefix() { return ");


                                            Write(Model.RowClassName);

WriteLiteral(".localTextPrefix; }\r\n\r\n        constructor(container: JQuery) {\r\n            supe" +
"r(container);\r\n        }\r\n    }\r\n}");


        }
    }
}
#pragma warning restore 1591
