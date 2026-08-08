import { render, screen } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { MessageBubble } from './MessageBubble'

describe('MessageBubble', () => {
  it('renders user messages as plain text without parsing markdown', () => {
    render(<MessageBubble role="user" text="**not bold**" />)

    // The raw markers stay visible — user input is never run through the
    // markdown renderer.
    expect(screen.getByText('**not bold**')).toBeInTheDocument()
    expect(document.querySelector('strong')).toBeNull()
  })

  it('renders assistant messages as markdown', () => {
    render(<MessageBubble role="assistant" text="**Hello** from assistant" />)

    expect(screen.getByText('Hello', { selector: 'strong' })).toBeInTheDocument()
  })
})
