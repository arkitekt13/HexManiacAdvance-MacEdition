using HavenSoft.HexManiac.Core.Models;
using System;
using System.Threading.Tasks;
using Xunit;

namespace HavenSoft.HexManiac.Tests {
   public class ControlledDispatchTests {
      [Fact]
      public void RunQueuedWork_CompletesSuccessfullyRatherThanCancelling() {
         var dispatcher = new ControlledDispatch();
         var ran = false;
         var task = dispatcher.DispatchWork(() => ran = true);
         Assert.False(task.IsCompleted);

         dispatcher.RunAllWorkloads();

         Assert.True(ran);
         Assert.True(task.IsCompletedSuccessfully);
         Assert.Equal(0, dispatcher.WorkloadCount);
      }

      [Fact]
      public void SynchronousContinuation_QueuesFollowup_DrainedBeforeReturn() {
         var dispatcher = new ControlledDispatch();
         var followupRan = false;
         var task = dispatcher.RunBackgroundWork(() => { });
         var continuation = task.ContinueWith(_ => dispatcher.DispatchWork(() => followupRan = true),
            TaskContinuationOptions.ExecuteSynchronously).Unwrap();

         dispatcher.RunAllWorkloads();

         Assert.True(continuation.IsCompletedSuccessfully);
         Assert.True(followupRan);
         Assert.Equal(0, dispatcher.WorkloadCount);
      }

      [Fact]
      public void ActionFailure_FaultsReturnedTaskAndPropagatesOriginalException() {
         var dispatcher = new ControlledDispatch();
         var expected = new InvalidOperationException("injected workload failure");
         var task = dispatcher.DispatchWork(() => throw expected);

         Assert.Same(expected, Assert.Throws<InvalidOperationException>(() => dispatcher.RunAllWorkloads()));
         Assert.True(task.IsFaulted);
         Assert.Same(expected, task.Exception.InnerException);
         Assert.Equal(0, dispatcher.WorkloadCount);
      }
   }
}
