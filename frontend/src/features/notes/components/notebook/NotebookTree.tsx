import { useState, useRef, useCallback } from 'react'
import { Search, Folder, FileText, Plus, Coffee, FolderOpen, Loader2 } from 'lucide-react'
import type { TreeNode } from '../../utils/buildTree'
import type { NotebookItem, Notebook } from '../../types'
import {
  useCreateNotebookItem,
  useUpdateNotebookItem,
  useDeleteNotebookItem,
  useReorderNotebookItems,
  useNotebookItems,
} from '../../hooks/useNotesQueries'
import { useDebounce } from '../../../../hooks/useDebounce'
import { useClickOutside } from '../../../../hooks/useClickOutside'
import { useToast } from '../../../../components/ui/useToast'
import TreeItem from './TreeItem'
import { findSiblings } from './findSiblings'
import SearchResultItem from './SearchResultItem'

interface NotebookTreeProps {
  notebook: Notebook
  notebookSlug: string
  tree: TreeNode[]
  activePage: NotebookItem | null
}

export default function NotebookTree({ notebook, notebookSlug, tree, activePage }: NotebookTreeProps) {
  const canEdit = !!notebook.canEdit
  const [searchInput, setSearchInput] = useState('')
  const debouncedSearch = useDebounce(searchInput, 300)
  const [showRootCreate, setShowRootCreate] = useState(false)
  const [draggingId, setDraggingId] = useState<string | null>(null)
  const rootMenuRef = useRef<HTMLDivElement>(null)

  const createItem = useCreateNotebookItem(notebook.id)
  const updateItem = useUpdateNotebookItem(notebook.id)
  const deleteItem = useDeleteNotebookItem(notebook.id)
  const reorderItems = useReorderNotebookItems(notebook.id)
  const { showToast: showTreeToast } = useToast()

  const {
    data: searchResults,
    isPending: searchPending,
    isError: searchError,
  } = useNotebookItems(notebook.id, debouncedSearch || undefined)

  const isSearching = debouncedSearch.length > 0

  useClickOutside(rootMenuRef, () => setShowRootCreate(false))

  const handleCreateRoot = (type: 'folder' | 'page') => {
    const title = type === 'folder' ? 'New Folder' : 'New Page'
    createItem.mutate(
      {
        parentId: null,
        type,
        title,
        sortOrder: 0,
        contentJson: type === 'page' ? { type: 'doc', content: [] } : null,
        plainTextContent: type === 'page' ? '' : null,
      },
      {
        onSuccess: () => {
          setShowRootCreate(false)
          showTreeToast('Item created')
        },
        onError: (err: unknown) => {
          const msg = err instanceof Error ? err.message : 'Failed to create'
          showTreeToast(msg, 'error')
        },
      },
    )
  }

  const handleCreateItem = useCallback(
    async (parentId: string | null, type: 'folder' | 'page') => {
      const title = type === 'folder' ? 'New Folder' : 'New Page'
      await createItem.mutateAsync({
        parentId,
        type,
        title,
        sortOrder: 0,
        contentJson: type === 'page' ? { type: 'doc', content: [] } : null,
        plainTextContent: type === 'page' ? '' : null,
      })
      showTreeToast('Item created')
    },
    [createItem, showTreeToast],
  )

  const handleRenameItem = useCallback(
    async (itemId: string, title: string, sortOrder: number) => {
      await updateItem.mutateAsync({
        itemId,
        data: { title, sortOrder },
      })
      showTreeToast('Renamed')
    },
    [updateItem, showTreeToast],
  )

  const handleDeleteItem = useCallback(
    async (itemId: string) => {
      await deleteItem.mutateAsync(itemId)
      showTreeToast('Deleted')
    },
    [deleteItem, showTreeToast],
  )

  const computeReorderPayload = useCallback(
    (siblings: TreeNode[]): { itemId: string; parentId: string | null; sortOrder: number }[] => {
      return siblings.map((node, idx) => ({
        itemId: node.item.id,
        parentId: node.item.parentId,
        sortOrder: idx * 10,
      }))
    },
    [],
  )

  const handleMoveUp = useCallback(
    (itemId: string) => {
      const { siblings, index } = findSiblings(tree, itemId)
      if (index <= 0 || siblings.length < 2) return
      const newSiblings = [...siblings]
      const temp = newSiblings[index - 1]
      newSiblings[index - 1] = newSiblings[index]
      newSiblings[index] = temp
      reorderItems.mutate({ items: computeReorderPayload(newSiblings) })
    },
    [tree, reorderItems, computeReorderPayload],
  )

  const handleMoveDown = useCallback(
    (itemId: string) => {
      const { siblings, index } = findSiblings(tree, itemId)
      if (index < 0 || index >= siblings.length - 1 || siblings.length < 2) return
      const newSiblings = [...siblings]
      const temp = newSiblings[index + 1]
      newSiblings[index + 1] = newSiblings[index]
      newSiblings[index] = temp
      reorderItems.mutate({ items: computeReorderPayload(newSiblings) })
    },
    [tree, reorderItems, computeReorderPayload],
  )

  const handleDropOnFolder = useCallback(
    (folderId: string) => {
      if (!draggingId || draggingId === folderId) {
        setDraggingId(null)
        return
      }
      reorderItems.mutate(
        {
          items: [
            {
              itemId: draggingId,
              parentId: folderId,
              sortOrder: 0,
            },
          ],
        },
        {
          onSettled: () => setDraggingId(null),
        },
      )
    },
    [draggingId, reorderItems],
  )

  const dragState = canEdit
    ? {
        draggingId,
        onDragStart: setDraggingId,
        onDragEnd: () => setDraggingId(null),
        onDropOnFolder: handleDropOnFolder,
      }
    : undefined

  return (
    <div className="flex flex-col h-full">
      {/* Header */}
      <div className="px-5 pt-5 pb-3">
        <div className="flex items-center gap-3 mb-1">
          <div className="h-8 w-8 rounded-lg bg-stone-100 flex items-center justify-center shrink-0">
            <FolderOpen className="h-4 w-4 text-brand-brown" />
          </div>
          <h2 className="text-sm font-bold text-black leading-tight line-clamp-2">{notebook.title}</h2>
        </div>
        <p className="text-xs text-gray-400 ml-11">
          {notebook.visibility === 'public' ? 'Public Notebook' : 'Private Notebook'}
        </p>
      </div>

      {/* Search */}
      <div className="px-4 pb-3">
        <label className="relative block">
          <span className="sr-only">Search in this notebook</span>
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-3.5 w-3.5 text-gray-400" />
          <input
            type="text"
            placeholder="Search in this notebook..."
            value={searchInput}
            onChange={(e) => setSearchInput(e.target.value)}
            className="w-full pl-8 pr-3 py-2 rounded-lg border border-gray-100 bg-gray-50 text-xs outline-none focus:bg-white focus:border-gray-200 transition-colors placeholder:text-gray-400"
          />
        </label>
      </div>

      {/* Root actions */}
      {canEdit && !isSearching && (
        <div className="px-4 pb-2">
          <div className="relative" ref={rootMenuRef}>
            <button
              onClick={() => setShowRootCreate(!showRootCreate)}
              className="w-full flex items-center justify-center gap-1.5 rounded-lg border border-dashed border-gray-200 px-3 py-1.5 text-xs text-gray-500 hover:border-gray-300 hover:text-gray-700 hover:bg-gray-50 transition-colors"
            >
              <Plus className="h-3.5 w-3.5" />
              Add folder or page
            </button>
            {showRootCreate && (
              <div className="absolute left-0 right-0 top-full mt-1 rounded-lg border border-gray-100 bg-white shadow-lg z-50 py-1">
                <button
                  onClick={() => handleCreateRoot('folder')}
                  className="w-full flex items-center gap-2 px-3 py-1.5 text-xs text-gray-700 hover:bg-gray-50 transition-colors"
                >
                  <Folder className="h-3.5 w-3.5 text-brand-brown" />
                  New folder
                </button>
                <button
                  onClick={() => handleCreateRoot('page')}
                  className="w-full flex items-center gap-2 px-3 py-1.5 text-xs text-gray-700 hover:bg-gray-50 transition-colors"
                >
                  <FileText className="h-3.5 w-3.5 text-gray-400" />
                  New page
                </button>
              </div>
            )}
          </div>
        </div>
      )}

      {/* Tree or Search Results */}
      <div className="flex-1 overflow-y-auto px-2 pb-4">
        {isSearching ? (
          <div>
            {searchPending ? (
              <div className="flex items-center justify-center py-8">
                <Loader2 className="h-4 w-4 animate-spin text-gray-400" />
              </div>
            ) : searchError ? (
              <p className="text-xs text-red-500 text-center py-8">Search failed. Please try again.</p>
            ) : !searchResults?.length ? (
              <p className="text-xs text-gray-400 text-center py-8">No results found.</p>
            ) : (
              <div className="space-y-0.5">
                {searchResults.map((item) => (
                  <SearchResultItem
                    key={item.id}
                    item={item}
                    notebookSlug={notebookSlug}
                    activePath={activePage?.path ?? null}
                  />
                ))}
              </div>
            )}
          </div>
        ) : (
          <>
            {tree.map((node, idx) => (
              <TreeItem
                key={node.item.id}
                node={node}
                notebookSlug={notebookSlug}
                activePath={activePage?.path ?? null}
                level={0}
                canEdit={canEdit}
                onMoveUp={handleMoveUp}
                onMoveDown={handleMoveDown}
                siblingCount={tree.length}
                index={idx}
                dragState={dragState}
                onCreateItem={handleCreateItem}
                onRenameItem={handleRenameItem}
                onDeleteItem={handleDeleteItem}
              />
            ))}
            {tree.length === 0 && (
              <div className="px-4 py-6 text-center">
                <p className="text-xs text-gray-400">This notebook is empty.</p>
                {canEdit && (
                  <p className="text-xs text-gray-400 mt-1">Add a folder or page to get started.</p>
                )}
              </div>
            )}
          </>
        )}
      </div>

      {/* Footer */}
      <div className="px-5 py-3 border-t border-gray-100">
        <div className="flex items-center gap-2 text-xs text-gray-400">
          <Coffee className="h-3.5 w-3.5" />
          <span>Powered by CodeCafe</span>
        </div>
      </div>
    </div>
  )
}
