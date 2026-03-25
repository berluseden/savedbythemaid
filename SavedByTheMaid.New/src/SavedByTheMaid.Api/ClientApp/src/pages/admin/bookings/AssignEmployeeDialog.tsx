import { User } from 'lucide-react';
import type { Employee, MeetingSummary } from './types';

interface AssignEmployeeDialogProps {
  meeting: MeetingSummary;
  employees: Employee[];
  onAssign: (meetingId: number, employeeId: number) => void;
  onCancel: () => void;
}

export function AssignEmployeeDialog({
  meeting,
  employees,
  onAssign,
  onCancel,
}: AssignEmployeeDialogProps) {
  return (
    <div className="pt-3 border-t border-gray-200">
      <p className="text-xs text-gray-500 mb-2">Assigned Employee</p>
      <div className="flex gap-2">
        <select
          onChange={(e) => onAssign(meeting.id, parseInt(e.target.value))}
          className="flex-1 px-3 py-2 border rounded-lg text-sm focus:ring-2 focus:ring-brand"
          defaultValue=""
        >
          <option value="" disabled>Select employee...</option>
          {employees.map((emp) => (
            <option key={emp.id} value={emp.id}>
              {emp.firstName} {emp.lastName}
            </option>
          ))}
        </select>
        <button
          onClick={onCancel}
          className="px-3 py-2 border rounded-lg hover:bg-gray-100"
        >
          Cancel
        </button>
      </div>
    </div>
  );
}

interface EmployeeDisplayProps {
  meeting: MeetingSummary;
  onStartAssign: () => void;
}

export function EmployeeDisplay({ meeting, onStartAssign }: EmployeeDisplayProps) {
  return (
    <div className="pt-3 border-t border-gray-200">
      <p className="text-xs text-gray-500 mb-2">Assigned Employee</p>
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-2">
          <User className="h-4 w-4 text-gray-400" />
          <span className="font-medium text-gray-900">
            {meeting.employeeName || 'Unassigned'}
          </span>
        </div>
        <button
          onClick={onStartAssign}
          className="text-sm text-brand hover:text-brand-dark font-medium"
        >
          {meeting.employeeId ? 'Change' : 'Assign'}
        </button>
      </div>
    </div>
  );
}
