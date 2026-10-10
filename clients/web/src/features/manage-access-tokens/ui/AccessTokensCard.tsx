import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useState } from 'react'
import type { FormEvent } from 'react'
import { useTranslation } from 'react-i18next'
import { formatRelativeTime } from '@/entities/notebook'
import {
  createPersonalAccessToken,
  listPersonalAccessTokens,
  revokePersonalAccessToken,
  sessionKeys,
} from '@/entities/session'
import type { CreatedPersonalAccessToken } from '@/entities/session'
import { Button, ConfirmButton, Input, Spinner } from '@/shared/ui'
import { TokenRevealDialog } from './TokenRevealDialog'

const EXPIRY_MIN_DAYS = 1
const EXPIRY_MAX_DAYS = 365

/**
 * Personal access tokens: list, create (with the one-time reveal), revoke.
 * Revoked rows stay visible but dimmed — hiding them would pretend they never
 * existed, which is the wrong audit story.
 */
export function AccessTokensCard() {
  const { t, i18n } = useTranslation()
  const queryClient = useQueryClient()
  const [name, setName] = useState('')
  const [expiryDays, setExpiryDays] = useState('')
  const [created, setCreated] = useState<CreatedPersonalAccessToken | null>(null)

  const tokens = useQuery({
    queryKey: sessionKeys.tokens,
    queryFn: ({ signal }) => listPersonalAccessTokens(signal),
  })

  const invalidate = () => {
    void queryClient.invalidateQueries({ queryKey: sessionKeys.tokens })
  }

  const create = useMutation({
    mutationFn: () => {
      const days = expiryDays.trim() === '' ? null : Number(expiryDays)
      return createPersonalAccessToken({ name: name.trim(), expiresInDays: days })
    },
    onSuccess: (token) => {
      setName('')
      setExpiryDays('')
      setCreated(token)
      invalidate()
    },
  })

  const revoke = useMutation({
    mutationFn: (id: string) => revokePersonalAccessToken(id),
    onSuccess: invalidate,
  })

  const parsedDays = expiryDays.trim() === '' ? null : Number(expiryDays)
  const expiryInvalid =
    parsedDays !== null &&
    (!Number.isInteger(parsedDays) || parsedDays < EXPIRY_MIN_DAYS || parsedDays > EXPIRY_MAX_DAYS)
  const canCreate = name.trim().length > 0 && !expiryInvalid && !create.isPending

  const submit = (event: FormEvent) => {
    event.preventDefault()
    if (canCreate) {
      create.mutate()
    }
  }

  return (
    <div className="flex flex-col gap-4">
      <p className="text-sm text-muted">{t('account.tokensIntro')}</p>

      {tokens.isPending ? (
        <div className="grid place-items-center py-6">
          <Spinner />
          <span className="sr-only">{t('list.loading')}</span>
        </div>
      ) : tokens.isError ? (
        <div className="text-center">
          <p className="text-sm text-muted">{t('list.loadError')}</p>
          <Button
            variant="ghost"
            className="mt-2"
            onClick={() => {
              void tokens.refetch()
            }}
          >
            {t('list.retry')}
          </Button>
        </div>
      ) : tokens.data.length === 0 ? (
        <p className="rounded-xl border border-dashed border-line px-3 py-4 text-center text-xs text-muted">
          {t('account.tokensEmpty')}
        </p>
      ) : (
        <ul className="divide-y divide-line rounded-xl border border-line">
          {tokens.data.map((token) => {
            const revoked = token.revokedAtUtc !== null
            return (
              <li
                key={token.id}
                className={`flex items-center gap-3 px-3 py-2.5 ${revoked ? 'opacity-50' : ''}`}
              >
                <div className="min-w-0 flex-1">
                  <p className="truncate text-sm font-medium text-ink">{token.name}</p>
                  <p className="text-xs text-muted">
                    {t('account.tokenCreated', {
                      time: formatRelativeTime(token.createdAtUtc, i18n.language),
                    })}
                    {' · '}
                    {t('account.tokenExpires', {
                      time: formatRelativeTime(token.expiresAtUtc, i18n.language),
                    })}
                  </p>
                </div>
                {revoked ? (
                  <span className="shrink-0 rounded-full border border-line px-2 py-px text-[11px] text-muted">
                    {t('account.tokenRevoked')}
                  </span>
                ) : (
                  <ConfirmButton
                    label={t('account.tokenRevoke')}
                    confirmLabel={t('account.tokenRevokeConfirm')}
                    busy={revoke.isPending}
                    onConfirm={() => {
                      revoke.mutate(token.id)
                    }}
                  />
                )}
              </li>
            )
          })}
        </ul>
      )}

      <form className="flex flex-col gap-2 sm:flex-row" onSubmit={submit}>
        <div className="min-w-0 flex-1">
          <Input
            type="text"
            value={name}
            onChange={(event) => {
              setName(event.target.value)
            }}
            maxLength={50}
            placeholder={t('account.tokenNamePlaceholder')}
            aria-label={t('account.tokenName')}
          />
        </div>
        <Input
          type="number"
          value={expiryDays}
          onChange={(event) => {
            setExpiryDays(event.target.value)
          }}
          min={EXPIRY_MIN_DAYS}
          max={EXPIRY_MAX_DAYS}
          invalid={expiryInvalid}
          placeholder={t('account.tokenExpiryPlaceholder')}
          aria-label={t('account.tokenExpiry')}
          className="sm:w-40"
        />
        <Button type="submit" disabled={!canCreate} className="shrink-0">
          {create.isPending ? t('account.tokenCreating') : t('account.tokenCreate')}
        </Button>
      </form>
      {create.isError ? (
        <p role="alert" className="text-sm text-accent-strong">
          {t('account.tokenCreateFailed')}
        </p>
      ) : null}

      {created !== null ? (
        <TokenRevealDialog
          token={created}
          onClose={() => {
            setCreated(null)
          }}
        />
      ) : null}
    </div>
  )
}
