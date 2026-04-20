import { useEffect, useState, useRef } from 'react';
import { useForm, Controller } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { Save, Phone, MapPin, Clock, ChevronDown, Check } from 'lucide-react';
import { AdminLayout } from '@/components/admin/AdminLayout';
import { businessInfoApi, type BusinessInfoDto } from '@/lib/api-endpoints';
import { Spinner } from '@/shared/components/ui/spinner';

const TIME_SLOTS: string[] = (() => {
  const slots: string[] = [];
  for (let h = 6; h <= 22; h++) {
    for (const m of [0, 30]) {
      if (h === 22 && m === 30) break;
      const h12 = h > 12 ? h - 12 : h === 0 ? 12 : h;
      const ampm = h < 12 ? 'AM' : 'PM';
      slots.push(`${h12}:${m === 0 ? '00' : '30'} ${ampm}`);
    }
  }
  return slots;
})();

function parseHoursStr(str: string): { start: string; end: string } {
  const parts = str.split(' \u2013 ');
  return {
    start: TIME_SLOTS.includes(parts[0]) ? parts[0] : '8:00 AM',
    end: TIME_SLOTS.includes(parts[1]) ? parts[1] : '6:00 PM',
  };
}

function toHoursStr(start: string, end: string) {
  return `${start} \u2013 ${end}`;
}

const schema = z.object({
  phone: z.string().max(30),
  email: z.string().email('Invalid email'),
  addressLine1: z.string().max(150),
  city: z.string().max(100),
  state: z.string().max(50),
  zipCode: z.string().max(10),
  responseTime: z.string().max(100),
  weekdayStart: z.string(),
  weekdayEnd: z.string(),
  saturdayOpen: z.boolean(),
  saturdayStart: z.string(),
  saturdayEnd: z.string(),
  sundayOpen: z.boolean(),
  sundayStart: z.string(),
  sundayEnd: z.string(),
});

type FormValues = z.infer<typeof schema>;

export function AdminSettingsPage() {
  const queryClient = useQueryClient();

  const { data, isLoading } = useQuery({
    queryKey: ['business-info'],
    queryFn: () => businessInfoApi.get(),
  });

  const { register, handleSubmit, reset, control, watch, formState: { errors, isDirty } } = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: {
      phone: '', email: '', addressLine1: '', city: '', state: '', zipCode: '', responseTime: '',
      weekdayStart: '8:00 AM', weekdayEnd: '6:00 PM',
      saturdayOpen: false, saturdayStart: '9:00 AM', saturdayEnd: '6:00 PM',
      sundayOpen: false, sundayStart: '9:00 AM', sundayEnd: '6:00 PM',
    },
  });

  const saturdayOpen = watch('saturdayOpen');
  const sundayOpen = watch('sundayOpen');

  useEffect(() => {
    if (!data?.data) return;
    const d = data.data;
    const wd = parseHoursStr(d.weekdayHours || '8:00 AM \u2013 6:00 PM');
    const sa = parseHoursStr(d.saturdayHours || '9:00 AM \u2013 6:00 PM');
    const su = parseHoursStr(d.sundayHours || '9:00 AM \u2013 6:00 PM');
    reset({
      phone: d.phone, email: d.email, addressLine1: d.addressLine1,
      city: d.city, state: d.state, zipCode: d.zipCode, responseTime: d.responseTime,
      weekdayStart: wd.start, weekdayEnd: wd.end,
      saturdayOpen: !!d.saturdayHours, saturdayStart: sa.start, saturdayEnd: sa.end,
      sundayOpen: !!d.sundayHours, sundayStart: su.start, sundayEnd: su.end,
    });
  }, [data, reset]);

  const mutation = useMutation({
    mutationFn: (values: FormValues) => {
      const payload: BusinessInfoDto = {
        phone: values.phone,
        email: values.email,
        addressLine1: values.addressLine1,
        city: values.city,
        state: values.state,
        zipCode: values.zipCode,
        responseTime: values.responseTime,
        weekdayHours: toHoursStr(values.weekdayStart, values.weekdayEnd),
        saturdayHours: values.saturdayOpen ? toHoursStr(values.saturdayStart, values.saturdayEnd) : '',
        sundayHours: values.sundayOpen ? toHoursStr(values.sundayStart, values.sundayEnd) : '',
      };
      return businessInfoApi.update(payload);
    },
    onSuccess: (res) => {
      queryClient.setQueryData(['business-info'], res);
      const d = res.data;
      const wd = parseHoursStr(d.weekdayHours || '8:00 AM \u2013 6:00 PM');
      const sa = parseHoursStr(d.saturdayHours || '9:00 AM \u2013 6:00 PM');
      const su = parseHoursStr(d.sundayHours || '9:00 AM \u2013 6:00 PM');
      reset({
        phone: d.phone, email: d.email, addressLine1: d.addressLine1,
        city: d.city, state: d.state, zipCode: d.zipCode, responseTime: d.responseTime,
        weekdayStart: wd.start, weekdayEnd: wd.end,
        saturdayOpen: !!d.saturdayHours, saturdayStart: sa.start, saturdayEnd: sa.end,
        sundayOpen: !!d.sundayHours, sundayStart: su.start, sundayEnd: su.end,
      });
    },
  });

  const onSubmit = (values: FormValues) => mutation.mutate(values);

  if (isLoading) {
    return <AdminLayout><div className="flex justify-center py-20"><Spinner /></div></AdminLayout>;
  }

  return (
    <AdminLayout>
      <div className="max-w-2xl">
        <div className="mb-6">
          <h1 className="text-2xl font-bold text-gray-900">Business Settings</h1>
          <p className="text-gray-500 mt-1 text-sm">This information appears on the public Contact page.</p>
        </div>

        <form onSubmit={handleSubmit(onSubmit)} className="space-y-8">

          {/* Contact info */}
          <section className="bg-white rounded-xl border border-gray-200 p-6 space-y-4">
            <h2 className="font-semibold text-gray-900 flex items-center gap-2">
              <Phone className="h-4 w-4 text-brand" />
              Contact Information
            </h2>
            <Field label="Phone number" error={errors.phone?.message}>
              <input {...register('phone')} className={inputCls(!!errors.phone)} placeholder="(555) 123-4567" />
            </Field>
            <Field label="Contact email" error={errors.email?.message}>
              <input {...register('email')} type="email" className={inputCls(!!errors.email)} placeholder="hello@ecomaid.com" />
            </Field>
            <Field label="Response time" error={errors.responseTime?.message}>
              <input {...register('responseTime')} className={inputCls(!!errors.responseTime)} placeholder="We reply within 24 hours" />
            </Field>
          </section>

          {/* Address */}
          <section className="bg-white rounded-xl border border-gray-200 p-6 space-y-4">
            <h2 className="font-semibold text-gray-900 flex items-center gap-2">
              <MapPin className="h-4 w-4 text-brand" />
              Office Address
            </h2>
            <Field label="Street address" error={errors.addressLine1?.message}>
              <input {...register('addressLine1')} className={inputCls(!!errors.addressLine1)} placeholder="123 Cleaning Street" />
            </Field>
            <div className="grid grid-cols-2 gap-4">
              <Field label="City" error={errors.city?.message}>
                <input {...register('city')} className={inputCls(!!errors.city)} placeholder="New York" />
              </Field>
              <Field label="State" error={errors.state?.message}>
                <input {...register('state')} className={inputCls(!!errors.state)} placeholder="NY" />
              </Field>
            </div>
            <Field label="ZIP code" error={errors.zipCode?.message}>
              <input {...register('zipCode')} className={inputCls(!!errors.zipCode)} placeholder="10001" />
            </Field>
          </section>

          {/* Business hours */}
          <section className="bg-white rounded-xl border border-gray-200 p-6 space-y-5">
            <h2 className="font-semibold text-gray-900 flex items-center gap-2">
              <Clock className="h-4 w-4 text-brand" />
              Business Hours
            </h2>

            {/* Monday – Friday */}
            <div>
              <p className="text-sm font-medium text-gray-700 mb-2">Monday – Friday</p>
              <div className="flex items-center gap-2">
                <Controller control={control} name="weekdayStart"
                  render={({ field }) => <TimeSelect value={field.value} onChange={field.onChange} />} />
                <span className="text-xs text-gray-400 shrink-0 px-1">to</span>
                <Controller control={control} name="weekdayEnd"
                  render={({ field }) => <TimeSelect value={field.value} onChange={field.onChange} />} />
              </div>
            </div>

            {/* Saturday */}
            <div>
              <div className="flex items-center justify-between mb-2">
                <p className="text-sm font-medium text-gray-700">Saturday</p>
                <Controller control={control} name="saturdayOpen"
                  render={({ field }) => (
                    <DayToggle checked={field.value} onChange={field.onChange} />
                  )} />
              </div>
              {saturdayOpen && (
                <div className="flex items-center gap-2">
                  <Controller control={control} name="saturdayStart"
                    render={({ field }) => <TimeSelect value={field.value} onChange={field.onChange} />} />
                  <span className="text-xs text-gray-400 shrink-0 px-1">to</span>
                  <Controller control={control} name="saturdayEnd"
                    render={({ field }) => <TimeSelect value={field.value} onChange={field.onChange} />} />
                </div>
              )}
            </div>

            {/* Sunday */}
            <div>
              <div className="flex items-center justify-between mb-2">
                <p className="text-sm font-medium text-gray-700">Sunday</p>
                <Controller control={control} name="sundayOpen"
                  render={({ field }) => (
                    <DayToggle checked={field.value} onChange={field.onChange} />
                  )} />
              </div>
              {sundayOpen && (
                <div className="flex items-center gap-2">
                  <Controller control={control} name="sundayStart"
                    render={({ field }) => <TimeSelect value={field.value} onChange={field.onChange} />} />
                  <span className="text-xs text-gray-400 shrink-0 px-1">to</span>
                  <Controller control={control} name="sundayEnd"
                    render={({ field }) => <TimeSelect value={field.value} onChange={field.onChange} />} />
                </div>
              )}
            </div>
          </section>

          {/* Save */}
          <div className="flex items-center gap-3">
            <button
              type="submit"
              disabled={!isDirty || mutation.isPending}
              className="flex items-center gap-2 px-5 py-2.5 bg-brand text-white rounded-lg text-sm font-medium hover:bg-brand/90 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
            >
              {mutation.isPending ? <Spinner className="h-4 w-4" /> : <Save className="h-4 w-4" />}
              Save changes
            </button>
            {mutation.isSuccess && <span className="text-sm text-green-600">Saved successfully.</span>}
            {mutation.isError && <span className="text-sm text-red-600">Failed to save. Please try again.</span>}
          </div>
        </form>
      </div>
    </AdminLayout>
  );
}

function TimeSelect({ value, onChange }: { value: string; onChange: (v: string) => void }) {
  const [open, setOpen] = useState(false);
  const ref = useRef<HTMLDivElement>(null);
  const listRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    const handler = (e: MouseEvent) => {
      if (ref.current && !ref.current.contains(e.target as Node)) setOpen(false);
    };
    document.addEventListener('mousedown', handler);
    return () => document.removeEventListener('mousedown', handler);
  }, []);

  useEffect(() => {
    if (open && listRef.current) {
      const idx = TIME_SLOTS.indexOf(value);
      const item = listRef.current.children[idx] as HTMLElement | undefined;
      item?.scrollIntoView({ block: 'nearest' });
    }
  }, [open, value]);

  return (
    <div ref={ref} className="relative flex-1">
      <button
        type="button"
        onClick={() => setOpen(!open)}
        className="flex items-center justify-between w-full px-3 py-2 text-sm border border-gray-200 rounded-lg bg-white hover:border-brand/40 focus:ring-2 focus:ring-brand/20 focus:border-brand outline-none transition-all"
      >
        <span className="font-medium text-gray-800">{value}</span>
        <ChevronDown className={`h-3.5 w-3.5 text-gray-400 transition-transform duration-150 ${open ? 'rotate-180' : ''}`} />
      </button>

      {open && (
        <div
          ref={listRef}
          className="absolute z-50 mt-1.5 w-full bg-white border border-gray-100 rounded-xl shadow-xl shadow-black/5 max-h-52 overflow-y-auto py-1"
        >
          {TIME_SLOTS.map((slot) => {
            const selected = slot === value;
            return (
              <button
                key={slot}
                type="button"
                onClick={() => { onChange(slot); setOpen(false); }}
                className={`flex items-center justify-between w-full px-3 py-1.5 text-sm transition-colors ${
                  selected
                    ? 'bg-brand/5 text-brand font-semibold'
                    : 'text-gray-700 hover:bg-gray-50'
                }`}
              >
                {slot}
                {selected && <Check className="h-3.5 w-3.5 text-brand" />}
              </button>
            );
          })}
        </div>
      )}
    </div>
  );
}

function DayToggle({ checked, onChange }: { checked: boolean; onChange: (v: boolean) => void }) {
  return (
    <button
      type="button"
      onClick={() => onChange(!checked)}
      className="flex items-center gap-2 group"
    >
      <span className={`text-xs font-medium transition-colors ${checked ? 'text-brand' : 'text-gray-400'}`}>
        {checked ? 'Open' : 'Closed'}
      </span>
      <div className={`relative w-9 h-5 rounded-full transition-colors duration-200 ${checked ? 'bg-brand' : 'bg-gray-200'}`}>
        <div className={`absolute top-0.5 left-0.5 w-4 h-4 bg-white rounded-full shadow-sm transition-transform duration-200 ${checked ? 'translate-x-4' : 'translate-x-0'}`} />
      </div>
    </button>
  );
}

function Field({ label, error, children }: { label: string; error?: string; children: React.ReactNode }) {
  return (
    <div>
      <label className="block text-sm font-medium text-gray-700 mb-1">{label}</label>
      {children}
      {error && <p className="text-xs text-red-600 mt-1">{error}</p>}
    </div>
  );
}

function inputCls(hasError: boolean) {
  return `w-full px-3 py-2 text-sm border rounded-lg focus:ring-2 focus:ring-brand focus:border-transparent outline-none transition-colors ${
    hasError ? 'border-red-400 bg-red-50' : 'border-gray-200 bg-white'
  }`;
}
