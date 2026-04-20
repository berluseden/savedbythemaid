import { useEffect } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { Save, Phone, Mail, MapPin, Clock } from 'lucide-react';
import { AdminLayout } from '@/components/admin/AdminLayout';
import { businessInfoApi, type BusinessInfoDto } from '@/lib/api-endpoints';
import { Spinner } from '@/shared/components/ui/spinner';

const schema = z.object({
  phone: z.string().max(30),
  email: z.string().email('Invalid email'),
  addressLine1: z.string().max(150),
  city: z.string().max(100),
  state: z.string().max(50),
  zipCode: z.string().max(10),
  weekdayHours: z.string().max(50),
  saturdayHours: z.string().max(50),
  sundayHours: z.string().max(50),
  responseTime: z.string().max(100),
});

type FormValues = z.infer<typeof schema>;

export function AdminSettingsPage() {
  const queryClient = useQueryClient();

  const { data, isLoading } = useQuery({
    queryKey: ['business-info'],
    queryFn: () => businessInfoApi.get(),
  });

  const { register, handleSubmit, reset, formState: { errors, isDirty } } = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: {
      phone: '',
      email: '',
      addressLine1: '',
      city: '',
      state: '',
      zipCode: '',
      weekdayHours: '',
      saturdayHours: '',
      sundayHours: '',
      responseTime: '',
    },
  });

  useEffect(() => {
    if (data?.data) reset(data.data as FormValues);
  }, [data, reset]);

  const mutation = useMutation({
    mutationFn: (values: FormValues) => businessInfoApi.update(values as BusinessInfoDto),
    onSuccess: (res) => {
      queryClient.setQueryData(['business-info'], res);
      reset(res.data as FormValues);
    },
  });

  const onSubmit = (values: FormValues) => mutation.mutate(values);

  if (isLoading) {
    return (
      <AdminLayout>
        <div className="flex justify-center py-20"><Spinner /></div>
      </AdminLayout>
    );
  }

  return (
    <AdminLayout>
      <div className="max-w-2xl">
        <div className="mb-6">
          <h1 className="text-2xl font-bold text-gray-900">Business Settings</h1>
          <p className="text-gray-500 mt-1 text-sm">
            This information appears on the public Contact page.
          </p>
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
          <section className="bg-white rounded-xl border border-gray-200 p-6 space-y-4">
            <h2 className="font-semibold text-gray-900 flex items-center gap-2">
              <Clock className="h-4 w-4 text-brand" />
              Business Hours
            </h2>
            <p className="text-xs text-gray-500">Format: "8:00 AM – 8:00 PM" or leave empty if closed.</p>

            <Field label="Monday – Friday" error={errors.weekdayHours?.message}>
              <input {...register('weekdayHours')} className={inputCls(!!errors.weekdayHours)} placeholder="8:00 AM – 8:00 PM" />
            </Field>

            <Field label="Saturday" error={errors.saturdayHours?.message}>
              <input {...register('saturdayHours')} className={inputCls(!!errors.saturdayHours)} placeholder="9:00 AM – 6:00 PM" />
            </Field>

            <Field label="Sunday" error={errors.sundayHours?.message}>
              <input {...register('sundayHours')} className={inputCls(!!errors.sundayHours)} placeholder="9:00 AM – 6:00 PM" />
            </Field>
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
            {mutation.isSuccess && (
              <span className="text-sm text-green-600">Saved successfully.</span>
            )}
            {mutation.isError && (
              <span className="text-sm text-red-600">Failed to save. Please try again.</span>
            )}
          </div>
        </form>
      </div>
    </AdminLayout>
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
