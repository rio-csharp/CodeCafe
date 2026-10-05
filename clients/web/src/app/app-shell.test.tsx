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

  expect(await screen.findByText('Every notebook, freshly brewed.')).toBeInTheDocument()
  expect(screen.getByText("Today's Menu")).toBeInTheDocument()
  expect(await screen.findByText("The barista hasn't started yet")).toBeInTheDocument()
  expect(screen.getByText('Brewed with ❤️ and caffeine')).toBeInTheDocument()
})

it('switches every visible string to Chinese without a reload', async () => {
  const user = userEvent.setup()
  render(<Providers />)

  await screen.findByText('Every notebook, freshly brewed.')
  await user.click(screen.getByRole('button', { name: 'Switch language' }))

  expect(await screen.findByText('每一本笔记，都是一杯现磨。')).toBeInTheDocument()
  expect(screen.getByText('今日菜单')).toBeInTheDocument()
  expect(screen.getByText('用 ❤️ 和咖啡因酿造')).toBeInTheDocument()
  expect(screen.getByRole('button', { name: '切换语言' })).toBeInTheDocument()
  expect(document.documentElement.lang).toBe('zh')
  expect(window.localStorage.getItem('codecafe.lang')).toBe('zh')
})
