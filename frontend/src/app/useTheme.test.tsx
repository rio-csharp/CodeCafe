import { render } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'
import { ThemeContext } from './themeContext'
import { useTheme } from './useTheme'

describe('useTheme', () => {
  it('throws when used outside ThemeProvider', () => {
    expect(() => render(<ThemeConsumer />)).toThrow('useTheme must be used within ThemeProvider')
  })

  it('returns the theme context when provided', () => {
    const setTheme = vi.fn()
    const view = render(
      <ThemeContext.Provider value={{ setTheme, theme: 'e-ink' }}>
        <ThemeConsumer />
      </ThemeContext.Provider>,
    )

    expect(view.getByText('theme:e-ink')).toBeInTheDocument()
  })
})

function ThemeConsumer() {
  const theme = useTheme()

  return <div>{`theme:${theme.theme}`}</div>
}
