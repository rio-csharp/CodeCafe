import { render, screen } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { MessageBubble } from './MessageBubble'

describe('MessageBubble', () => {
  it('uses dark-mode-safe styling for user messages', () => {
    const { container } = render(<MessageBubble role="user" text="Hello from user" />)

    const bubble = container.querySelector('.rounded-md')
    expect(bubble).toHaveClass('bg-brand-brown')
    expect(bubble).toHaveClass('dark:bg-brand-brown-light')
    expect(bubble).toHaveClass('dark:text-surface')
  })

  it('keeps assistant markdown content inheriting the bubble text color', () => {
    render(<MessageBubble role="assistant" text="**Hello** from assistant" />)

    const markdown = screen.getByText('Hello', { selector: 'strong' }).closest('.prose')
    expect(markdown).not.toBeNull()
    expect(markdown).toHaveClass('text-inherit')
    expect(markdown).toHaveClass('prose-p:text-inherit')
    expect(markdown).toHaveClass('prose-headings:text-inherit')
  })
})
