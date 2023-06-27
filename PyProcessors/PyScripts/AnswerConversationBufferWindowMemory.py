#workaround from https://github.com/hwchase17/langchain/issues/5630#issuecomment-1574222564
from typing import Any, Dict
from langchain.chains.conversation.memory import ConversationBufferWindowMemory

class AnswerConversationBufferWindowMemory(ConversationBufferWindowMemory):
    def save_context(self, inputs: Dict[str, Any], outputs: Dict[str, str]) -> None:
        return super(AnswerConversationBufferWindowMemory, self).save_context(inputs,{'response': outputs['answer']})