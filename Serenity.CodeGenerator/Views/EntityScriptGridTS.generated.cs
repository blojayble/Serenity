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
    public partial class EntityScriptGridTS : RazorGenerator.Templating.RazorTemplateBase
    {
#line hidden
 public dynamic Model { get; set; } 
        public override void Execute()
        {


WriteLiteral("\r\n");



                                                   
    var dotModule = Model.Module == null ? "" : ("." + Model.Module);
    var moduleDot = Model.Module == null ? "" : (Model.Module + ".");
    var moduleSlash = Model.Module == null ? "" : (Model.Module + "/");


WriteLiteral("namespace ");


      Write(Model.RootNamespace);


                            Write(dotModule);

WriteLiteral(" {\r\n    import D = Serenity.Decorators;\r\n\r\n    ");


WriteLiteral("@D.columnsKey(\"");


                Write(moduleDot);


                            Write(Model.ClassName);


                                                  WriteLiteral("\")");

                                                     if (Model.Identity != null) {
WriteLiteral(" ");


WriteLiteral("@D.idProperty(");


                                                                                                    Write(Model.RowClassName);

WriteLiteral(".idProperty)");


                                                                                                                                                }

                                                                                                                                                  if (Model.NameField != null){
WriteLiteral(" ");


WriteLiteral("@D.nameProperty(");


                                                                                                                                                                                                   Write(Model.RowClassName);

WriteLiteral(".nameProperty)");


                                                                                                                                                                                                                                                 }

WriteLiteral("\r\n    ");


WriteLiteral("@D.dialogType(");


               Write(Model.ClassName);

WriteLiteral("Dialog) ");


WriteLiteral("@D.localTextPrefix(");


                                                             Write(Model.RowClassName);

WriteLiteral(".localTextPrefix) ");


WriteLiteral("@D.service(");


                                                                                                                Write(Model.ClassName);

WriteLiteral("Service.baseUrl)\r\n    export class ");


             Write(Model.ClassName);

WriteLiteral("Grid extends Serenity.EntityGrid<");


                                                                Write(Model.RowClassName);

WriteLiteral(", any> {\r\n        constructor(container: JQuery) {\r\n            super(container);" +
"\r\n        }\r\n    }\r\n}");


        }
    }
}
#pragma warning restore 1591
