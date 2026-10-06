const HOME = '/'

/**
 * Where to land after a successful login/register: the page the user was sent
 * away from, if the gate left one behind in `location.state.from`.
 */
export function redirectTo(state: unknown): string {
  if (typeof state === 'object' && state !== null && 'from' in state) {
    const { from } = state as { from?: unknown }
    if (typeof from === 'string' && from.length > 0) {
      return from
    }
  }
  return HOME
}
