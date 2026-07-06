/**
 * If the URL currently points at `oldPath` (or a descendant of it, for the
 * folder-rename/move case), rewrite it to the corresponding tail under
 * `newPath` and `replace` the history entry. No-op when the path didn't
 * actually change or the URL isn't under the notebook's prefix.
 *
 * The descendant case: e.g. URL tail is `folder/oldName/sub/page` and
 * `oldPath` is `folder/oldName`, then `newPath` is `folder/oldName-renamed`
 * and we want the new tail to be `folder/oldName-renamed/sub/page`. The
 * `currentTail.slice(oldPath.length)` swap preserves the descendant suffix.
 */
export function syncUrlToPathChange(
  oldPath: string,
  newPath: string,
  notebookSlug: string,
  locationPathname: string,
  navigate: (path: string, opts?: { replace?: boolean }) => void,
): void {
  if (!oldPath || oldPath === newPath) return
  const prefix = `/notes/${notebookSlug}/`
  if (!locationPathname.startsWith(prefix)) return
  const currentTail = locationPathname.slice(prefix.length)
  if (currentTail === oldPath || currentTail.startsWith(`${oldPath}/`)) {
    const newTail = newPath + currentTail.slice(oldPath.length)
    navigate(`${prefix}${newTail}`, { replace: true })
  }
}
