#workaround from https://github.com/hwchase17/langchain/issues/5630#issuecomment-1574222564
from typing import Any, Dict
from langchain.chains.conversation.memory import ConversationBufferMemory

class AnswerConversationBufferMemory(ConversationBufferMemory):
    def save_context(self, inputs: Dict[str, Any], outputs: Dict[str, str]) -> None:
        return super(AnswerConversationBufferMemory, self).save_context(inputs,{'response': outputs['answer']})