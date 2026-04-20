using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CJ.Plug.Models.Plug
{
    public enum PlugCreateTypeEnum
    {
        SystemInitPlug, //系统初始化插头，由依赖注入创建的插头
        SystemInitActionPlug, //系统初始化插头动作，由依赖注入创建的插头动作
        RootPlug, //根插头，由用户在插头库创建的插头
        RootAdminPlug, //根插头，由管理员在插头库创建的插头，所有人可见，普通用户不可删除
        //------以上为Root类插头，展示在插头管理页面------//

        ProcessToPlug, //在流程管理界面创建的插头
        InstancePlug, //实例插头，由用户从组件库拖入流程图时创建的插头
        ActionRootPlug, //动作插头，由用户在组件库动作配置时创建的插头
        ActionInstancePlug, //动作实例插头，由用户在组件配置时添加到组件执行动作时创建的插头

        //UserInitPlug, //用户初始化插头        
        //SystemPlug, //系统插头
        //UserPlug, //用户插头        
        //SystemComponent, //系统组件
        //UserComponent, //用户组件
        //ProcessComponent, //流程组件
        //SystemCustomTool, //系统自定义工具
        //UserCustomTool, //用户自定义工具
        //ProcessCustomTool, //流程自定义工具
        //SystemCustomComponent, //系统自定义组件
        //UserCustomComponent, //用户自定义组件
        //ProcessCustomComponent, //流程自定义组件
        //ProcessCustomPlug, //流程自定义插头
        None, //无
        新建流程
    }
}
