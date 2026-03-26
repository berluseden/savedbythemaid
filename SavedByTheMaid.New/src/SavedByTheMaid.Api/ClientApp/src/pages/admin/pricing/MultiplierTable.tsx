import { useState } from 'react';
import { Edit2, Trash2, Percent, Settings } from 'lucide-react';
import { type PriceMultiplier, CONDITION_TYPES } from './types';

interface MultiplierTableProps {
  multipliers: PriceMultiplier[];
  onEdit: (m: PriceMultiplier) => void;
  onDelete: (id: number) => void;
  onCreateClick: () => void;
}

function formatFactor(factor: number) {
  const pct = ((factor - 1) * 100).toFixed(0);
  if (factor > 1) return `+${pct}%`;
  if (factor < 1) return `${pct}%`;
  return 'Base (x1.0)';
}

export function MultiplierTable({ multipliers, onEdit, onDelete, onCreateClick }: MultiplierTableProps) {
  const [deleteConfirm, setDeleteConfirm] = useState<number | null>(null);

  const grouped = CONDITION_TYPES.map(ct => ({
    ...ct,
    items: multipliers.filter(m => m.conditionType === ct.value),
  })).filter(g => g.items.length > 0);

  return (
    <>
      <div className="space-y-6">
        {grouped.length === 0 ? (
          <div className="text-center py-12 bg-white rounded-xl border">
            <Settings className="h-12 w-12 text-gray-300 mx-auto mb-4" />
            <p className="text-gray-500">No price multipliers configured</p>
            <button
              onClick={onCreateClick}
              className="mt-4 text-brand hover:underline"
            >
              Create the first one
            </button>
          </div>
        ) : (
          grouped.map((group) => (
            <div key={group.value} className="bg-white rounded-xl shadow-sm border">
              <div className="px-6 py-4 border-b bg-gray-50 rounded-t-xl">
                <h3 className="font-semibold text-gray-900">{group.label}</h3>
                <p className="text-sm text-gray-500">{group.description}</p>
              </div>
              <div className="overflow-x-auto">
                <table className="w-full">
                  <thead>
                    <tr className="border-b">
                      <th className="text-left py-3 px-6 font-medium text-gray-600 text-sm">Name</th>
                      <th className="text-left py-3 px-6 font-medium text-gray-600 text-sm">Range</th>
                      <th className="text-left py-3 px-6 font-medium text-gray-600 text-sm">Factor</th>
                      <th className="text-left py-3 px-6 font-medium text-gray-600 text-sm">Applies To</th>
                      <th className="text-left py-3 px-6 font-medium text-gray-600 text-sm">Status</th>
                      <th className="text-right py-3 px-6 font-medium text-gray-600 text-sm">Actions</th>
                    </tr>
                  </thead>
                  <tbody>
                    {group.items.map((m) => (
                      <tr key={m.id} className={`border-b last:border-0 ${!m.isActive ? 'opacity-50' : ''}`}>
                        <td className="py-4 px-6">
                          <p className="font-medium text-gray-900">{m.name}</p>
                          {m.description && <p className="text-xs text-gray-500">{m.description}</p>}
                        </td>
                        <td className="py-4 px-6 text-sm text-gray-600">
                          {m.minValue != null || m.maxValue != null ? (
                            <>
                              {m.minValue != null ? m.minValue : '\u2014'} {'\u2192'} {m.maxValue != null ? m.maxValue : '\u221E'}
                            </>
                          ) : (
                            <span className="text-gray-400">{'\u2014'}</span>
                          )}
                        </td>
                        <td className="py-4 px-6">
                          <span className={`inline-flex items-center gap-1 px-2 py-1 rounded font-medium text-sm ${
                            m.factor > 1
                              ? 'bg-amber-50 text-amber-700'
                              : m.factor < 1
                                ? 'bg-green-50 text-green-700'
                                : 'bg-gray-50 text-gray-700'
                          }`}>
                            <Percent className="h-3 w-3" />
                            {formatFactor(m.factor)} (x{m.factor})
                          </span>
                        </td>
                        <td className="py-4 px-6 text-sm">
                          <div className="flex gap-2">
                            {m.appliesToPrice && (
                              <span className="px-2 py-0.5 bg-blue-50 text-blue-700 text-xs rounded">Price</span>
                            )}
                            {m.appliesToTime && (
                              <span className="px-2 py-0.5 bg-purple-50 text-purple-700 text-xs rounded">Time</span>
                            )}
                          </div>
                        </td>
                        <td className="py-4 px-6">
                          <span className={`text-sm ${m.isActive ? 'text-green-600' : 'text-gray-400'}`}>
                            {m.isActive ? 'Active' : 'Inactive'}
                          </span>
                        </td>
                        <td className="py-4 px-6 text-right">
                          <div className="flex items-center justify-end gap-1">
                            <button
                              onClick={() => onEdit(m)}
                              className="p-2 text-gray-400 hover:text-brand hover:bg-accent-light/10 rounded-lg"
                            >
                              <Edit2 className="h-4 w-4" />
                            </button>
                            <button
                              onClick={() => setDeleteConfirm(m.id)}
                              className="p-2 text-gray-400 hover:text-red-500 hover:bg-red-50 rounded-lg"
                            >
                              <Trash2 className="h-4 w-4" />
                            </button>
                          </div>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </div>
          ))
        )}
      </div>

      {/* Delete Confirmation */}
      {deleteConfirm !== null && (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/50">
          <div className="bg-white rounded-xl shadow-xl p-6 w-full max-w-sm">
            <h3 className="text-lg font-semibold text-gray-900 mb-2">Delete multiplier?</h3>
            <p className="text-gray-600 mb-6">This action cannot be undone.</p>
            <div className="flex gap-3">
              <button
                onClick={() => setDeleteConfirm(null)}
                className="flex-1 px-4 py-2 border border-gray-300 text-gray-700 rounded-lg hover:bg-gray-50"
              >
                Cancel
              </button>
              <button
                onClick={() => {
                  onDelete(deleteConfirm);
                  setDeleteConfirm(null);
                }}
                className="flex-1 px-4 py-2 bg-red-500 text-white rounded-lg hover:bg-red-600"
              >
                Delete
              </button>
            </div>
          </div>
        </div>
      )}
    </>
  );
}
