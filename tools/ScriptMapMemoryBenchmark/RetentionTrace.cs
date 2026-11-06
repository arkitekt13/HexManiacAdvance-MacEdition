using System.Collections;
using System.Reflection;
using Avalonia;
using Avalonia.VisualTree;
using Avalonia.LogicalTree;

// Investigation only. Ignore weak handles and primitive buffers; report strong paths
// from the supplied document to a watched detached control or one of its children.
static class RetentionTrace {
   public static string Find(object root, Visual target) {
      var queue = new Queue<(object Value, string Path)>();
      var seen = new HashSet<object>(ReferenceEqualityComparer.Instance);
      queue.Enqueue((root, "document"));
      int count=0;
      while(queue.Count>0 && count++<300000) {
         var (value,path)=queue.Dequeue();
         if(value==null || !seen.Add(value))continue;
         var type=value.GetType();
         if(value is Visual visual) {
            if(ReferenceEquals(visual,target)||visual.GetVisualAncestors().Contains(target)||visual.GetLogicalAncestors().Contains(target))return path+" ["+type.Name+"]";
            if(root is not Visual)continue;
         }
         if(value is Delegate callback) {
            int i=0;foreach(var action in callback.GetInvocationList())queue.Enqueue((action.Target,path+" -> "+action.Method.Name+".Target["+(i++)+"]"));
            continue;
         }
         if(type.IsPrimitive||type.IsEnum||value is string||value is Type||value is MemberInfo||value is WeakReference||type.IsGenericType&&type.GetGenericTypeDefinition()==typeof(WeakReference<>))continue;
         if(value is Array array) {
            if(type.GetElementType().IsPrimitive)continue;
            int i=0;foreach(var item in array){if(i>=4096)break;queue.Enqueue((item,path+"["+(i++)+"]"));}continue;
         }
         if(value is IEnumerable enumerable && value is not HavenSoft.HexManiac.Core.Models.BaseModel && value is not HavenSoft.HexManiac.Core.ViewModels.IViewPort) {
            int i=0;try{foreach(var item in enumerable){if(i>=4096)break;queue.Enqueue((item,path+".item["+(i++)+"]"));}}catch{}
         }
         for(var current=type;current!=null;current=current.BaseType)foreach(var field in current.GetFields(BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.DeclaredOnly)) {
            if(field.FieldType.IsPrimitive||field.FieldType.IsEnum||field.FieldType.IsPointer)continue;
            try{queue.Enqueue((field.GetValue(value),path+"."+field.Name));}catch{}
         }
      }
      return "No path within "+count+" visited nodes";
   }
}
