import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { beforeEach, expect, it, vi } from 'vitest'
import { Providers } from './providers'

beforeEach(() => {
  vi.stubGlobal(
    'fetch',
    vi.fn(
      async () =>
        new Response(
          JSON.stringify({
            value: { items: [], page: 1, pageSize: 12, totalCount: 0, hasNextPage: false },
            error: null,
            isSuccess: true,
          }),
          { status: 200, headers: { 'content-type': 'application/json' } },
        ),
    ),
  )
})

it('mounts the homepage shell with real copy', async () => {
  render(<Providers />)

  expect(await screen.findByText('Write notebooks here, then share them.')).toBeInTheDocument()
  expect(await screen.findByText('No public notebooks yet.')).toBeInTheDocument()
  expect(screen.getByRole('link', { name: 'Create account' })).toBeInTheDocument()
})

it('switches every visible string to Chinese without a reload', async () => {
  const user = userEvent.setup()
  render(<Providers />)

  await screen.findByText('Write notebooks here, then share them.')
  await user.click(screen.getByRole('button', { name: 'Switch language' }))

  expect(await screen.findByText('在这里写笔记，再把它们分享出去。')).toBeInTheDocument()
  expect(screen.getByText('暂时没有公开的笔记本。')).toBeInTheDocument()
  expect(screen.getByRole('button', { name: '切换语言' })).toBeInTheDocument()
  expect(document.documentElement.lang).toBe('zh')
  expect(window.localStorage.getItem('codecafe.lang')).toBe('zh')
})
