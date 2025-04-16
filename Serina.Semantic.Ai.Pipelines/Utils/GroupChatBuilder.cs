
using AutoGen.Core;

namespace Serina.Semantic.Ai.Pipelines.Utils
{
    public  class GroupChatBuilder
    {
        private GroupChatBuilder()
        {                
        }         

        public static GroupChatBuilder New { get { return new GroupChatBuilder(); } }
        private readonly List<Transition> transitions = new List<Transition>(); 
        private readonly Graph graph;  

        public GroupChatBuilder AddTransition(Transition transition)
        {
            transitions.Add(transition);
            return this;
        }

        public GroupChat Build()
        {
            if (!transitions.Any())
            {
                throw new InvalidOperationException("transitions are empty");
            }

            var workflow = new Graph( transitions.ToArray() );

            var memeber = new List<IAgent>();

            foreach (var transition in transitions)
            {
                if (!memeber.Any(m=>m.Name == transition.From.Name))
                {
                    memeber.Add(transition.From);
                }
                if (!memeber.Any(m => m.Name == transition.To.Name))
                {
                    memeber.Add(transition.To);
                }


            }


             
            var groupChat = new GroupChat(
                admin: transitions.First().From,
                workflow: workflow,
                members: memeber);




            return groupChat;   
        }


    }
}