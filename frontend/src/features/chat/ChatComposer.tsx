export function ChatComposer({
  draftMessage,
  isSending,
  onDraftMessageChange,
  onSendMessage,
  onStopGeneration,
}: {
  draftMessage: string
  isSending: boolean
  onDraftMessageChange: (value: string) => void
  onSendMessage: () => void
  onStopGeneration: () => void
}) {
  return (
    <form
      className="chat-composer"
      onSubmit={(event) => {
        event.preventDefault()
        onSendMessage()
      }}
    >
      <label className="sr-only" htmlFor="chat-message">
        Message
      </label>
      <div className="chat-composer-inputs">
        <textarea
          id="chat-message"
          name="message"
          onChange={(event) => onDraftMessageChange(event.target.value)}
          onKeyDown={(event) => {
            if (event.key === 'Enter' && !event.shiftKey) {
              event.preventDefault()
              onSendMessage()
            }
          }}
          placeholder="Ask about code, notes, deployments, or architecture..."
          rows={1}
          value={draftMessage}
        />
      </div>

      <div className="chat-composer-actions">
        {isSending ? (
          <button
            aria-label="Stop generation"
            className="chat-composer-primary"
            onClick={onStopGeneration}
            title="Stop generation"
            type="button"
          >
            Stop
          </button>
        ) : (
          <button
            aria-label="Send message"
            className="chat-composer-primary"
            title="Send message"
            type="submit"
          >
            <span aria-hidden="true">➤</span>
          </button>
        )}
      </div>
    </form>
  )
}
