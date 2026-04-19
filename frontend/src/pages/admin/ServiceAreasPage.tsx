import { useState, useEffect, useRef, useId } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import {
  Plus,
  Search,
  Edit2,
  Trash2,
  MapPin,
  X,
  ChevronDown,
  ChevronUp,
  CheckCircle,
  Globe,
  AlertTriangle,
  Building2,
} from 'lucide-react';
import { AdminLayout } from '../../components/admin/AdminLayout';
import api from '../../lib/api';
import {
  serviceAreaSchema,
  type ServiceAreaFormData,
} from '@/shared/schemas/admin.schema';
import {
  AlertDialog,
  AlertDialogContent,
  AlertDialogHeader,
  AlertDialogTitle,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogAction,
  AlertDialogCancel,
} from '@/shared/components/ui/alert-dialog';
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
} from '@/shared/components/ui/dialog';
import { Spinner } from '@/shared/components/ui/spinner';

interface ServiceAreaZip {
  id: number;
  zipCode: string;
  serviceAreaId: number;
}

interface ServiceArea {
  id: number;
  name: string;
  description: string | null;
  isActive: boolean;
  zipCodes: ServiceAreaZip[];
}

interface ZippopotamResponse {
  'country abbreviation': string;
  places: Array<{
    'place name': string;
    'state abbreviation': string;
    'post code': string;
  }>;
}

interface CityZipFetchState {
  city: string;
  state: string;
  status: 'idle' | 'loading' | 'success' | 'error';
  zips: string[];
  errorMsg: string;
}

interface NominatimPlace {
  display_name: string;
  address: {
    city?: string;
    town?: string;
    village?: string;
    suburb?: string;
    state?: string;
    state_code?: string;
    country_code?: string;
  };
}

const defaultCityFetch = (): CityZipFetchState => ({
  city: '',
  state: '',
  status: 'idle',
  zips: [],
  errorMsg: '',
});

const editDefaultValues: ServiceAreaFormData = {
  name: '',
  description: '',
  isActive: true,
};

const US_STATES = [
  { abbr: 'AL', name: 'Alabama' }, { abbr: 'AK', name: 'Alaska' },
  { abbr: 'AZ', name: 'Arizona' }, { abbr: 'AR', name: 'Arkansas' },
  { abbr: 'CA', name: 'California' }, { abbr: 'CO', name: 'Colorado' },
  { abbr: 'CT', name: 'Connecticut' }, { abbr: 'DE', name: 'Delaware' },
  { abbr: 'FL', name: 'Florida' }, { abbr: 'GA', name: 'Georgia' },
  { abbr: 'HI', name: 'Hawaii' }, { abbr: 'ID', name: 'Idaho' },
  { abbr: 'IL', name: 'Illinois' }, { abbr: 'IN', name: 'Indiana' },
  { abbr: 'IA', name: 'Iowa' }, { abbr: 'KS', name: 'Kansas' },
  { abbr: 'KY', name: 'Kentucky' }, { abbr: 'LA', name: 'Louisiana' },
  { abbr: 'ME', name: 'Maine' }, { abbr: 'MD', name: 'Maryland' },
  { abbr: 'MA', name: 'Massachusetts' }, { abbr: 'MI', name: 'Michigan' },
  { abbr: 'MN', name: 'Minnesota' }, { abbr: 'MS', name: 'Mississippi' },
  { abbr: 'MO', name: 'Missouri' }, { abbr: 'MT', name: 'Montana' },
  { abbr: 'NE', name: 'Nebraska' }, { abbr: 'NV', name: 'Nevada' },
  { abbr: 'NH', name: 'New Hampshire' }, { abbr: 'NJ', name: 'New Jersey' },
  { abbr: 'NM', name: 'New Mexico' }, { abbr: 'NY', name: 'New York' },
  { abbr: 'NC', name: 'North Carolina' }, { abbr: 'ND', name: 'North Dakota' },
  { abbr: 'OH', name: 'Ohio' }, { abbr: 'OK', name: 'Oklahoma' },
  { abbr: 'OR', name: 'Oregon' }, { abbr: 'PA', name: 'Pennsylvania' },
  { abbr: 'RI', name: 'Rhode Island' }, { abbr: 'SC', name: 'South Carolina' },
  { abbr: 'SD', name: 'South Dakota' }, { abbr: 'TN', name: 'Tennessee' },
  { abbr: 'TX', name: 'Texas' }, { abbr: 'UT', name: 'Utah' },
  { abbr: 'VT', name: 'Vermont' }, { abbr: 'VA', name: 'Virginia' },
  { abbr: 'WA', name: 'Washington' }, { abbr: 'WV', name: 'West Virginia' },
  { abbr: 'WI', name: 'Wisconsin' }, { abbr: 'WY', name: 'Wyoming' },
] as const;

const TOP_CITIES_BY_STATE: Record<string, string[]> = {
  AL: ['Birmingham', 'Montgomery', 'Huntsville', 'Mobile', 'Tuscaloosa'],
  AK: ['Anchorage', 'Fairbanks', 'Juneau', 'Sitka', 'Ketchikan'],
  AZ: ['Phoenix', 'Tucson', 'Mesa', 'Chandler', 'Scottsdale', 'Gilbert', 'Tempe', 'Glendale', 'Peoria'],
  AR: ['Little Rock', 'Fort Smith', 'Fayetteville', 'Springdale', 'Jonesboro'],
  CA: ['Los Angeles', 'San Diego', 'San Jose', 'San Francisco', 'Fresno', 'Sacramento', 'Long Beach', 'Oakland', 'Bakersfield', 'Anaheim', 'Santa Ana', 'Irvine', 'Riverside'],
  CO: ['Denver', 'Colorado Springs', 'Aurora', 'Fort Collins', 'Lakewood', 'Boulder', 'Pueblo'],
  CT: ['Bridgeport', 'New Haven', 'Hartford', 'Stamford', 'Waterbury', 'Norwalk'],
  DE: ['Wilmington', 'Dover', 'Newark', 'Middletown'],
  FL: ['Miami', 'Orlando', 'Tampa', 'Jacksonville', 'Fort Lauderdale', 'Naples', 'Sarasota', 'Boca Raton', 'St. Petersburg', 'Hialeah', 'Tallahassee', 'Miami Beach', 'Cape Coral', 'Palm Bay', 'Gainesville'],
  GA: ['Atlanta', 'Augusta', 'Columbus', 'Savannah', 'Athens', 'Sandy Springs', 'Macon', 'Roswell'],
  HI: ['Honolulu', 'Hilo', 'Kailua', 'Pearl City', 'Waipahu'],
  ID: ['Boise', 'Nampa', 'Meridian', 'Idaho Falls', 'Pocatello'],
  IL: ['Chicago', 'Aurora', 'Naperville', 'Joliet', 'Rockford', 'Springfield', 'Peoria', 'Elgin'],
  IN: ['Indianapolis', 'Fort Wayne', 'Evansville', 'South Bend', 'Carmel', 'Bloomington'],
  IA: ['Des Moines', 'Cedar Rapids', 'Davenport', 'Sioux City', 'Iowa City'],
  KS: ['Wichita', 'Overland Park', 'Kansas City', 'Topeka', 'Olathe'],
  KY: ['Louisville', 'Lexington', 'Bowling Green', 'Owensboro', 'Covington'],
  LA: ['New Orleans', 'Baton Rouge', 'Shreveport', 'Lafayette', 'Lake Charles'],
  ME: ['Portland', 'Lewiston', 'Bangor', 'South Portland', 'Auburn'],
  MD: ['Baltimore', 'Frederick', 'Rockville', 'Gaithersburg', 'Bowie', 'Annapolis'],
  MA: ['Boston', 'Worcester', 'Springfield', 'Lowell', 'Cambridge', 'New Bedford', 'Brockton', 'Quincy'],
  MI: ['Detroit', 'Grand Rapids', 'Warren', 'Sterling Heights', 'Ann Arbor', 'Lansing', 'Flint', 'Dearborn'],
  MN: ['Minneapolis', 'Saint Paul', 'Rochester', 'Duluth', 'Bloomington', 'Brooklyn Park'],
  MS: ['Jackson', 'Gulfport', 'Southaven', 'Hattiesburg', 'Biloxi'],
  MO: ['Kansas City', 'Saint Louis', 'Springfield', 'Columbia', 'Independence'],
  MT: ['Billings', 'Missoula', 'Great Falls', 'Bozeman', 'Butte'],
  NE: ['Omaha', 'Lincoln', 'Bellevue', 'Grand Island', 'Kearney'],
  NV: ['Las Vegas', 'Henderson', 'Reno', 'North Las Vegas', 'Sparks'],
  NH: ['Manchester', 'Nashua', 'Concord', 'Derry', 'Dover'],
  NJ: ['Newark', 'Jersey City', 'Paterson', 'Elizabeth', 'Edison', 'Trenton', 'Clifton', 'Camden'],
  NM: ['Albuquerque', 'Las Cruces', 'Rio Rancho', 'Santa Fe', 'Roswell'],
  NY: ['New York', 'Buffalo', 'Yonkers', 'Rochester', 'Syracuse', 'Albany', 'New Rochelle'],
  NC: ['Charlotte', 'Raleigh', 'Greensboro', 'Durham', 'Winston-Salem', 'Fayetteville', 'Cary', 'Wilmington'],
  ND: ['Fargo', 'Bismarck', 'Grand Forks', 'Minot', 'West Fargo'],
  OH: ['Columbus', 'Cleveland', 'Cincinnati', 'Toledo', 'Akron', 'Dayton', 'Parma'],
  OK: ['Oklahoma City', 'Tulsa', 'Norman', 'Broken Arrow', 'Lawton'],
  OR: ['Portland', 'Salem', 'Eugene', 'Gresham', 'Hillsboro', 'Beaverton'],
  PA: ['Philadelphia', 'Pittsburgh', 'Allentown', 'Erie', 'Reading', 'Scranton', 'Bethlehem'],
  RI: ['Providence', 'Cranston', 'Warwick', 'Pawtucket', 'East Providence'],
  SC: ['Columbia', 'Charleston', 'North Charleston', 'Mount Pleasant', 'Greenville', 'Myrtle Beach'],
  SD: ['Sioux Falls', 'Rapid City', 'Aberdeen', 'Brookings', 'Watertown'],
  TN: ['Nashville', 'Memphis', 'Knoxville', 'Chattanooga', 'Clarksville', 'Murfreesboro'],
  TX: ['Houston', 'San Antonio', 'Dallas', 'Austin', 'Fort Worth', 'El Paso', 'Arlington', 'Corpus Christi', 'Plano', 'Laredo', 'Lubbock', 'Irving', 'Garland'],
  UT: ['Salt Lake City', 'West Valley City', 'Provo', 'West Jordan', 'Orem', 'Sandy'],
  VT: ['Burlington', 'South Burlington', 'Rutland', 'Barre', 'Montpelier'],
  VA: ['Virginia Beach', 'Norfolk', 'Chesapeake', 'Richmond', 'Newport News', 'Alexandria', 'Hampton'],
  WA: ['Seattle', 'Spokane', 'Tacoma', 'Vancouver', 'Bellevue', 'Kent', 'Everett', 'Renton'],
  WV: ['Charleston', 'Huntington', 'Parkersburg', 'Morgantown', 'Wheeling'],
  WI: ['Milwaukee', 'Madison', 'Green Bay', 'Kenosha', 'Racine', 'Appleton'],
  WY: ['Cheyenne', 'Casper', 'Laramie', 'Gillette', 'Rock Springs'],
};

function cn(...classes: (string | boolean | undefined | null)[]): string {
  return classes.filter(Boolean).join(' ');
}

// Inline toggle switch component
interface ToggleSwitchProps {
  checked: boolean;
  onChange: (checked: boolean) => void;
  label?: string;
  disabled?: boolean;
}

function ToggleSwitch({ checked, onChange, label, disabled }: ToggleSwitchProps) {
  return (
    <button
      type="button"
      role="switch"
      aria-checked={checked}
      aria-label={label ?? (checked ? 'Deactivate zone' : 'Activate zone')}
      disabled={disabled}
      onClick={(e) => {
        e.stopPropagation();
        onChange(!checked);
      }}
      className={cn(
        'relative inline-flex h-5 w-9 shrink-0 cursor-pointer rounded-full border-2 border-transparent transition-colors focus:outline-none focus-visible:ring-2 focus-visible:ring-brand',
        checked ? 'bg-brand' : 'bg-gray-200',
        disabled && 'cursor-not-allowed opacity-50',
      )}
    >
      <span
        className={cn(
          'inline-block h-4 w-4 rounded-full bg-white shadow transition-transform',
          checked ? 'translate-x-4' : 'translate-x-0',
        )}
      />
    </button>
  );
}

/** Calls zippopotam.us and returns ZIP codes for the given city+state. Throws on 404/error. */
async function fetchZipsForCity(city: string, state: string): Promise<string[]> {
  const encodedCity = encodeURIComponent(city.trim());
  const encodedState = encodeURIComponent(state.trim().toUpperCase());
  const res = await fetch(`https://api.zippopotam.us/us/${encodedState}/${encodedCity}`);
  if (res.status === 404) {
    throw new Error('City not found. Check spelling and state code.');
  }
  if (!res.ok) {
    throw new Error('Failed to fetch ZIP codes. Please try again.');
  }
  const data: ZippopotamResponse = await res.json() as ZippopotamResponse;
  return data.places.map((p) => p['post code']);
}

/** Preview label: "Found 6 ZIPs: 33139, 33140, 33141... +3 more" */
function ZipPreviewLabel({ zips }: { zips: string[] }) {
  const MAX_SHOW = 5;
  const shown = zips.slice(0, MAX_SHOW);
  const extra = zips.length - shown.length;
  return (
    <p className="text-sm text-success font-medium">
      Found {zips.length} ZIP{zips.length !== 1 ? 's' : ''}: {shown.join(', ')}
      {extra > 0 && <span className="text-gray-500"> +{extra} more</span>}
    </p>
  );
}

/** Small reusable 2-step city selector: pick state → pick city from pre-loaded list. */
interface CityFetchFormProps {
  value: CityZipFetchState;
  onChange: (next: CityZipFetchState) => void;
  onConfirm?: () => void;
  confirmLabel?: string;
  confirmLoading?: boolean;
}

function CityFetchForm({
  value,
  onChange,
  onConfirm,
  confirmLabel = 'Add ZIPs',
  confirmLoading = false,
}: CityFetchFormProps) {
  const formId = useId();
  const [selectedState, setSelectedState] = useState('');
  const [showStateDropdown, setShowStateDropdown] = useState(false);
  const [cityQuery, setCityQuery] = useState('');
  const [suggestions, setSuggestions] = useState<{ city: string; state: string; label: string }[]>([]);
  const [showDropdown, setShowDropdown] = useState(false);
  const [loadingSuggestions, setLoadingSuggestions] = useState(false);
  const debounceRef = useRef<ReturnType<typeof setTimeout> | null>(null);
  const containerRef = useRef<HTMLDivElement | null>(null);
  const stateContainerRef = useRef<HTMLDivElement | null>(null);

  useEffect(() => {
    const handleMouseDown = (e: MouseEvent) => {
      if (containerRef.current && !containerRef.current.contains(e.target as Node)) {
        setShowDropdown(false);
      }
      if (stateContainerRef.current && !stateContainerRef.current.contains(e.target as Node)) {
        setShowStateDropdown(false);
      }
    };
    const handleKeyDown = (e: KeyboardEvent) => {
      if (e.key === 'Escape') { setShowDropdown(false); setShowStateDropdown(false); }
    };
    document.addEventListener('mousedown', handleMouseDown);
    document.addEventListener('keydown', handleKeyDown);
    return () => {
      document.removeEventListener('mousedown', handleMouseDown);
      document.removeEventListener('keydown', handleKeyDown);
    };
  }, []);

  const presetForState = (abbr: string) =>
    (TOP_CITIES_BY_STATE[abbr] ?? []).map((city) => ({
      city,
      state: abbr,
      label: `${city}, ${abbr}`,
    }));

  const handleStateChange = (abbr: string) => {
    setSelectedState(abbr);
    setCityQuery('');
    onChange(defaultCityFetch());
    setSuggestions(abbr ? presetForState(abbr) : []);
    setShowDropdown(false);
  };

  const handleCityFocus = () => {
    if (!cityQuery && suggestions.length > 0) setShowDropdown(true);
  };

  const handleCityChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const val = e.target.value;
    setCityQuery(val);
    if (debounceRef.current) clearTimeout(debounceRef.current);

    if (!val.trim()) {
      const preset = selectedState ? presetForState(selectedState) : [];
      setSuggestions(preset);
      setShowDropdown(preset.length > 0);
      return;
    }

    const lower = val.toLowerCase();
    const preset = selectedState
      ? presetForState(selectedState).filter((s) => s.city.toLowerCase().startsWith(lower))
      : [];
    setSuggestions(preset);
    setShowDropdown(preset.length > 0);

    debounceRef.current = setTimeout(async () => {
      setLoadingSuggestions(true);
      try {
        const stateParam = selectedState ? `+${selectedState}` : '';
        const res = await fetch(
          `https://nominatim.openstreetmap.org/search?q=${encodeURIComponent(val + stateParam)}&countrycodes=us&featureclass=P&format=json&limit=10&addressdetails=1`,
        );
        const data = await res.json() as NominatimPlace[];
        const remote = data
          .filter(
            (p) =>
              p.address?.state_code &&
              (!selectedState || p.address.state_code.toUpperCase() === selectedState) &&
              (p.address.city || p.address.town || p.address.village),
          )
          .map((p) => {
            const city = p.address.city ?? p.address.town ?? p.address.village ?? '';
            const state = (p.address.state_code ?? '').toUpperCase();
            return { city, state, label: `${city}, ${state}` };
          })
          .filter((r, i, arr) => arr.findIndex((x) => x.label === r.label) === i);
        const presetLabels = new Set(preset.map((p) => p.label));
        const merged = [...preset, ...remote.filter((r) => !presetLabels.has(r.label))];
        setSuggestions(merged);
        setShowDropdown(merged.length > 0);
      } catch {
        // ignore
      } finally {
        setLoadingSuggestions(false);
      }
    }, 300);
  };

  const handleFetchForCity = async (city: string, state: string) => {
    onChange({ ...defaultCityFetch(), city, state, status: 'loading' });
    try {
      const zips = await fetchZipsForCity(city, state);
      onChange({ city, state, status: 'success', zips, errorMsg: '' });
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : 'Unknown error';
      onChange({ city, state, status: 'error', zips: [], errorMsg: msg });
    }
  };

  const handleSelect = (suggestion: { city: string; state: string; label: string }) => {
    setCityQuery(suggestion.label);
    setSuggestions([]);
    setShowDropdown(false);
    handleFetchForCity(suggestion.city, suggestion.state);
  };

  const selectedStateName = US_STATES.find((s) => s.abbr === selectedState)?.name;

  return (
    <div className="space-y-3">
      {/* Step 1: Custom state dropdown */}
      <div>
        <label className="block text-xs font-medium text-gray-500 mb-1">State</label>
        <div className="relative" ref={stateContainerRef}>
          <button
            type="button"
            onClick={() => setShowStateDropdown((v) => !v)}
            className={cn(
              'flex w-full items-center justify-between rounded-lg border px-3 py-2.5 text-sm transition-colors bg-white',
              showStateDropdown
                ? 'border-brand ring-1 ring-brand'
                : 'border-gray-300 hover:border-gray-400',
            )}
          >
            <div className="flex items-center gap-2">
              <Globe className="h-4 w-4 text-gray-400 shrink-0" />
              <span className={selectedState ? 'text-gray-900' : 'text-gray-400'}>
                {selectedState ? `${selectedStateName} (${selectedState})` : 'Select a state…'}
              </span>
            </div>
            <ChevronDown
              className={cn(
                'h-4 w-4 text-gray-400 transition-transform duration-150',
                showStateDropdown && 'rotate-180',
              )}
            />
          </button>

          {showStateDropdown && (
            <ul className="absolute z-50 mt-1 w-full rounded-lg border border-gray-200 bg-white shadow-lg text-sm max-h-56 overflow-y-auto">
              {US_STATES.map((s) => (
                <li
                  key={s.abbr}
                  onMouseDown={(e) => {
                    e.preventDefault();
                    setShowStateDropdown(false);
                    handleStateChange(s.abbr);
                  }}
                  className={cn(
                    'cursor-pointer px-3 py-2 transition-colors',
                    selectedState === s.abbr
                      ? 'bg-brand/10 text-brand font-medium'
                      : 'hover:bg-gray-50 text-gray-700',
                  )}
                >
                  {s.name}
                  <span className="ml-1 text-gray-400 text-xs">({s.abbr})</span>
                </li>
              ))}
            </ul>
          )}
        </div>
      </div>

      {/* Step 2: City combobox — visible only after state is selected */}
      {selectedState && (
        <div>
          <label htmlFor={`${formId}-city`} className="block text-xs font-medium text-gray-500 mb-1">City</label>
          <div className="relative" ref={containerRef}>
            <div className="absolute left-3 top-1/2 -translate-y-1/2 pointer-events-none">
              <Search className="h-4 w-4 text-gray-400" />
            </div>
            <input
              id={`${formId}-city`}
              name="city"
              type="text"
              placeholder="Browse or type to filter…"
              value={cityQuery}
              onChange={handleCityChange}
              onFocus={handleCityFocus}
              className="w-full rounded-lg border border-gray-300 pl-9 pr-3 py-2.5 text-sm focus:border-brand focus:outline-none focus:ring-1 focus:ring-brand"
            />
            {loadingSuggestions && (
              <div className="absolute right-3 top-1/2 -translate-y-1/2">
                <Spinner size="sm" />
              </div>
            )}
            {showDropdown && suggestions.length > 0 && (
              <ul className="absolute z-50 mt-1 w-full rounded-lg border border-gray-200 bg-white shadow-lg text-sm max-h-48 overflow-y-auto">
                {suggestions.map((s) => (
                  <li
                    key={s.label}
                    onMouseDown={(e) => {
                      e.preventDefault();
                      handleSelect(s);
                    }}
                    className="cursor-pointer px-3 py-2 hover:bg-gray-50 text-gray-700"
                  >
                    {s.city}
                    <span className="ml-1 text-gray-400 text-xs">{s.state}</span>
                  </li>
                ))}
              </ul>
            )}
          </div>
        </div>
      )}

      {/* Feedback */}
      {value.status === 'loading' && (
        <div className="flex items-center gap-2 text-sm text-gray-500">
          <Spinner size="sm" />
          <span>Fetching ZIPs…</span>
        </div>
      )}
      {value.status === 'success' && <ZipPreviewLabel zips={value.zips} />}
      {value.status === 'error' && (
        <p className="text-sm text-danger">{value.errorMsg}</p>
      )}

      {/* Confirm button */}
      {onConfirm && value.status === 'success' && value.zips.length > 0 && (
        <button
          type="button"
          disabled={confirmLoading}
          onClick={onConfirm}
          className="inline-flex items-center gap-1.5 rounded-lg bg-brand px-4 py-2 text-sm font-medium text-white hover:bg-brand-dark disabled:opacity-50 transition-colors"
        >
          {confirmLoading ? (
            <>
              <Spinner size="sm" />
              <span>Adding…</span>
            </>
          ) : (
            confirmLabel
          )}
        </button>
      )}
    </div>
  );
}

export function AdminServiceAreasPage() {
  const [serviceAreas, setServiceAreas] = useState<ServiceArea[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [searchTerm, setSearchTerm] = useState('');
  const [expandedArea, setExpandedArea] = useState<number | null>(null);

  // Optimistic toggle tracking: areaId -> pending state
  const [togglingIds, setTogglingIds] = useState<Set<number>>(new Set());

  // Modal state — null = closed, 'create' = create, ServiceArea = edit
  const [modalMode, setModalMode] = useState<null | 'create' | ServiceArea>(null);

  // Create modal city-fetch state
  const [createFetch, setCreateFetch] = useState<CityZipFetchState>(defaultCityFetch());
  const [createSubmitting, setCreateSubmitting] = useState(false);
  const [createDescription, setCreateDescription] = useState('');

  // Edit modal form (react-hook-form for edit only)
  const editForm = useForm<ServiceAreaFormData>({
    resolver: zodResolver(serviceAreaSchema),
    defaultValues: editDefaultValues,
  });

  // Inline ZIP input state per area
  const [zipInputs, setZipInputs] = useState<Record<number, string>>({});
  const [zipErrors, setZipErrors] = useState<Record<number, string>>({});
  const [zipAdding, setZipAdding] = useState<Record<number, boolean>>({});
  const [zipBulkMsg, setZipBulkMsg] = useState<Record<number, string>>({});
  const zipInputRefs = useRef<Record<number, HTMLInputElement | null>>({});

  // Expanded "Add city ZIPs" inline panel per area
  const [cityPanelOpen, setCityPanelOpen] = useState<Record<number, boolean>>({});
  const [cityPanelFetch, setCityPanelFetch] = useState<Record<number, CityZipFetchState>>({});
  const [cityPanelAdding, setCityPanelAdding] = useState<Record<number, boolean>>({});
  const [cityPanelMsg, setCityPanelMsg] = useState<Record<number, string>>({});

  // Delete confirmation state
  const [deleteAreaId, setDeleteAreaId] = useState<number | null>(null);
  const [deleteZip, setDeleteZip] = useState<{ areaId: number; zipId: number; zipCode: string } | null>(null);

  useEffect(() => {
    fetchServiceAreas();
  }, []);

  const fetchServiceAreas = async () => {
    setIsLoading(true);
    try {
      const response = await api.get<ServiceArea[]>('/serviceareas');
      setServiceAreas(response.data);
    } catch {
      // Error handled by API interceptor
    } finally {
      setIsLoading(false);
    }
  };

  // ── Create modal ──────────────────────────────────────────────────────────

  const openCreateModal = () => {
    setCreateFetch(defaultCityFetch());
    setCreateDescription('');
    setCreateSubmitting(false);
    setModalMode('create');
  };

  const handleCreateSubmit = async () => {
    const city = createFetch.city.trim();
    const state = createFetch.state.trim().toUpperCase();

    if (!city || !state) return;
    if (createFetch.status !== 'success' || createFetch.zips.length === 0) return;

    setCreateSubmitting(true);
    try {
      const zoneName = `${city}, ${state}`;
      const res = await api.post<ServiceArea>('/serviceareas', {
        name: zoneName,
        description: createDescription || null,
        isActive: true,
      });
      const newAreaId = res.data.id;

      // Bulk-add all ZIPs in parallel — ignore 409s (already assigned)
      await Promise.all(
        createFetch.zips.map((zip) =>
          api
            .post(`/serviceareas/${newAreaId}/zipcodes`, { zipCode: zip })
            .catch((err: unknown) => {
              const maybe = err as { response?: { status?: number } };
              if (maybe.response?.status !== 409) throw err;
            }),
        ),
      );

      setModalMode(null);
      fetchServiceAreas();
    } catch {
      // Error handled by API interceptor
    } finally {
      setCreateSubmitting(false);
    }
  };

  // ── Edit modal ────────────────────────────────────────────────────────────

  const openEditModal = (area: ServiceArea) => {
    editForm.reset({
      name: area.name,
      description: area.description || '',
      isActive: area.isActive,
    });
    setModalMode(area);
  };

  const handleEditSubmit = editForm.handleSubmit(async (data) => {
    const area = modalMode as ServiceArea;
    try {
      await api.put(`/serviceareas/${area.id}`, data);
      setModalMode(null);
      fetchServiceAreas();
    } catch {
      // Error handled by API interceptor
    }
  });

  const handleCloseModal = () => {
    setModalMode(null);
    editForm.reset(editDefaultValues);
  };

  // ── Delete / toggle ───────────────────────────────────────────────────────

  const handleDelete = async (id: number) => {
    try {
      await api.delete(`/serviceareas/${id}`);
      setDeleteAreaId(null);
      fetchServiceAreas();
    } catch {
      // Error handled by API interceptor
    }
  };

  const handleToggleActive = async (area: ServiceArea, newValue: boolean) => {
    setServiceAreas((prev) =>
      prev.map((a) => (a.id === area.id ? { ...a, isActive: newValue } : a)),
    );
    setTogglingIds((prev) => new Set(prev).add(area.id));
    try {
      await api.put(`/serviceareas/${area.id}`, {
        name: area.name,
        description: area.description,
        isActive: newValue,
      });
    } catch {
      setServiceAreas((prev) =>
        prev.map((a) => (a.id === area.id ? { ...a, isActive: area.isActive } : a)),
      );
    } finally {
      setTogglingIds((prev) => {
        const next = new Set(prev);
        next.delete(area.id);
        return next;
      });
    }
  };

  // ── ZIP delete ────────────────────────────────────────────────────────────

  const handleDeleteZipCode = async (areaId: number, zipId: number) => {
    try {
      await api.delete(`/serviceareas/${areaId}/zipcodes/${zipId}`);
      setDeleteZip(null);
      fetchServiceAreas();
    } catch {
      // Error handled by API interceptor
    }
  };

  // ── Inline ZIP input (manual add / paste) ────────────────────────────────

  const handleZipInputChange = (areaId: number, value: string) => {
    const digits = value.replace(/\D/g, '').slice(0, 5);
    setZipInputs((prev) => ({ ...prev, [areaId]: digits }));
    setZipErrors((prev) => ({ ...prev, [areaId]: '' }));
    setZipBulkMsg((prev) => ({ ...prev, [areaId]: '' }));
  };

  const addSingleZip = async (areaId: number, zip: string): Promise<boolean> => {
    try {
      await api.post(`/serviceareas/${areaId}/zipcodes`, { zipCode: zip });
      return true;
    } catch (error: unknown) {
      const maybe = error as { response?: { status?: number } };
      if (maybe.response?.status === 409) {
        setZipErrors((prev) => ({
          ...prev,
          [areaId]: `${zip} is already assigned to another zone`,
        }));
      } else {
        setZipErrors((prev) => ({ ...prev, [areaId]: 'Error adding zip code' }));
      }
      return false;
    }
  };

  const handleZipKeyDown = async (
    e: React.KeyboardEvent<HTMLInputElement>,
    areaId: number,
  ) => {
    if (e.key !== 'Enter') return;
    e.preventDefault();

    const zip = (zipInputs[areaId] ?? '').trim();
    if (!zip) return;

    if (!/^\d{5}$/.test(zip)) {
      setZipErrors((prev) => ({ ...prev, [areaId]: 'ZIP must be exactly 5 digits' }));
      return;
    }

    setZipAdding((prev) => ({ ...prev, [areaId]: true }));
    const ok = await addSingleZip(areaId, zip);
    setZipAdding((prev) => ({ ...prev, [areaId]: false }));

    if (ok) {
      setZipInputs((prev) => ({ ...prev, [areaId]: '' }));
      fetchServiceAreas();
    }
  };

  const handleZipPaste = async (
    e: React.ClipboardEvent<HTMLInputElement>,
    areaId: number,
  ) => {
    const text = e.clipboardData.getData('text');
    if (!text.includes(',') && !text.includes('\n') && !text.includes(' ')) return;

    e.preventDefault();
    const zips = text
      .split(/[,\n\s]+/)
      .map((z) => z.trim())
      .filter((z) => /^\d{5}$/.test(z));

    if (zips.length === 0) {
      setZipErrors((prev) => ({
        ...prev,
        [areaId]: 'No valid 5-digit ZIP codes found in pasted text',
      }));
      return;
    }

    setZipAdding((prev) => ({ ...prev, [areaId]: true }));
    setZipErrors((prev) => ({ ...prev, [areaId]: '' }));
    setZipBulkMsg((prev) => ({ ...prev, [areaId]: '' }));

    const results = await Promise.all(zips.map((z) => addSingleZip(areaId, z)));
    const addedCount = results.filter(Boolean).length;

    setZipAdding((prev) => ({ ...prev, [areaId]: false }));

    if (addedCount > 0) {
      setZipBulkMsg((prev) => ({
        ...prev,
        [areaId]: `Added ${addedCount} ZIP${addedCount !== 1 ? 's' : ''}`,
      }));
      fetchServiceAreas();
    }
  };

  // ── Expanded row "Add city ZIPs" panel ───────────────────────────────────

  const toggleCityPanel = (areaId: number) => {
    const isNowOpen = !cityPanelOpen[areaId];
    if (isNowOpen && !cityPanelFetch[areaId]) {
      setCityPanelFetch((p) => ({ ...p, [areaId]: defaultCityFetch() }));
    }
    setCityPanelOpen((prev) => ({ ...prev, [areaId]: !prev[areaId] }));
    setCityPanelMsg((prev) => ({ ...prev, [areaId]: '' }));
  };

  const handleCityPanelConfirm = async (areaId: number) => {
    const fetchState = cityPanelFetch[areaId];
    if (!fetchState || fetchState.zips.length === 0) return;

    setCityPanelAdding((prev) => ({ ...prev, [areaId]: true }));
    setCityPanelMsg((prev) => ({ ...prev, [areaId]: '' }));

    const results = await Promise.all(
      fetchState.zips.map((zip) =>
        api
          .post(`/serviceareas/${areaId}/zipcodes`, { zipCode: zip })
          .then(() => true)
          .catch((err: unknown) => {
            const maybe = err as { response?: { status?: number } };
            if (maybe.response?.status === 409) return false; // already exists — not a new add
            return false;
          }),
      ),
    );

    const added = results.filter(Boolean).length;
    setCityPanelAdding((prev) => ({ ...prev, [areaId]: false }));
    setCityPanelFetch((prev) => ({ ...prev, [areaId]: defaultCityFetch() }));
    setCityPanelOpen((prev) => ({ ...prev, [areaId]: false }));
    setCityPanelMsg((prev) => ({
      ...prev,
      [areaId]:
        added === 0
          ? 'All ZIPs already exist'
          : `Added ${added} new ZIP${added !== 1 ? 's' : ''}`,
    }));
    fetchServiceAreas();
  };

  // ── Filtering / sorting ───────────────────────────────────────────────────

  const toggleExpand = (areaId: number) => {
    setExpandedArea(expandedArea === areaId ? null : areaId);
  };

  const filteredAreas = serviceAreas
    .filter(
      (area) =>
        area.name.toLowerCase().includes(searchTerm.toLowerCase()) ||
        area.description?.toLowerCase().includes(searchTerm.toLowerCase()) ||
        area.zipCodes.some((z) => z.zipCode.includes(searchTerm)),
    )
    .sort((a, b) => {
      if (a.isActive !== b.isActive) return a.isActive ? -1 : 1;
      return a.name.localeCompare(b.name);
    });

  const totalZipCodes = serviceAreas.reduce((sum, area) => sum + area.zipCodes.length, 0);
  const activeAreas = serviceAreas.filter((a) => a.isActive).length;

  // ── Render ────────────────────────────────────────────────────────────────

  return (
    <AdminLayout>
      <div className="space-y-6">
        {/* Page header */}
        <div className="mb-8 flex items-center justify-between">
          <div>
            <h1 className="text-2xl font-bold text-gray-900">Service Areas</h1>
            <p className="mt-1 text-sm text-gray-500">Manage coverage zones by city</p>
          </div>
          <button
            onClick={openCreateModal}
            className="inline-flex items-center gap-2 rounded-lg bg-brand px-4 py-2 text-sm font-medium text-white hover:bg-brand-dark transition-colors"
          >
            <Plus className="h-4 w-4" />
            New Zone
          </button>
        </div>

        {/* Stats */}
        <div className="grid grid-cols-1 gap-4 sm:grid-cols-3">
          <div className="rounded-xl border border-gray-100 bg-white p-5 shadow-sm">
            <div className="flex items-center justify-between mb-4">
              <p className="text-sm text-gray-500">Total zones</p>
              <div className="h-9 w-9 rounded-lg bg-brand/10 flex items-center justify-center">
                <Globe className="h-4 w-4 text-brand" />
              </div>
            </div>
            <p className="text-2xl font-bold text-gray-900">{serviceAreas.length}</p>
          </div>
          <div className="rounded-xl border border-gray-100 bg-white p-5 shadow-sm">
            <div className="flex items-center justify-between mb-4">
              <p className="text-sm text-gray-500">Active zones</p>
              <div className="h-9 w-9 rounded-lg bg-brand/10 flex items-center justify-center">
                <CheckCircle className="h-4 w-4 text-brand" />
              </div>
            </div>
            <p className="text-2xl font-bold text-gray-900">{activeAreas}</p>
          </div>
          <div className="rounded-xl border border-gray-100 bg-white p-5 shadow-sm">
            <div className="flex items-center justify-between mb-4">
              <p className="text-sm text-gray-500">ZIP Codes</p>
              <div className="h-9 w-9 rounded-lg bg-brand/10 flex items-center justify-center">
                <MapPin className="h-4 w-4 text-brand" />
              </div>
            </div>
            <p className="text-2xl font-bold text-gray-900">{totalZipCodes}</p>
          </div>
        </div>

        {/* Search */}
        <div className="relative">
          <Search className="absolute left-3 top-1/2 h-5 w-5 -translate-y-1/2 text-gray-400" />
          <input
            type="text"
            name="search"
            aria-label="Search service areas by city, description or ZIP code"
            placeholder="Search by city, description or ZIP code..."
            value={searchTerm}
            onChange={(e) => setSearchTerm(e.target.value)}
            className="w-full rounded-lg border border-gray-300 bg-white py-2 pl-10 pr-4 text-sm focus:border-brand focus:outline-none focus:ring-1 focus:ring-brand"
          />
        </div>

        {/* Service Areas List */}
        {isLoading ? (
          <div className="flex items-center justify-center py-12">
            <Spinner size="md" />
          </div>
        ) : filteredAreas.length === 0 ? (
          <div className="rounded-lg border border-gray-200 bg-white p-12 text-center">
            <MapPin className="mx-auto h-12 w-12 text-gray-400" />
            <h3 className="mt-4 text-lg font-medium text-gray-900">No service areas</h3>
            <p className="mt-2 text-sm text-gray-500">
              {searchTerm
                ? 'No zones matching your search were found'
                : 'Start by creating your first service area'}
            </p>
            {!searchTerm && (
              <button
                onClick={openCreateModal}
                className="mt-4 inline-flex items-center gap-2 rounded-lg bg-brand px-4 py-2 text-sm font-medium text-white hover:bg-brand-dark"
              >
                <Plus className="h-4 w-4" />
                New Zone
              </button>
            )}
          </div>
        ) : (
          <div className="space-y-3">
            {filteredAreas.map((area) => {
              const isExpanded = expandedArea === area.id;
              const isEmpty = area.zipCodes.length === 0;
              const isToggling = togglingIds.has(area.id);

              return (
                <div
                  key={area.id}
                  className={cn(
                    'rounded-xl border bg-white overflow-hidden shadow-sm transition-opacity',
                    area.isActive ? 'border-gray-200' : 'border-gray-200 opacity-60',
                  )}
                >
                  {/* Area Header */}
                  <div className="flex items-center justify-between p-4">
                    {/* Left: expand chevron + info */}
                    <div className="flex items-center gap-3 min-w-0">
                      <button
                        onClick={() => toggleExpand(area.id)}
                        className="shrink-0 rounded-lg p-1 hover:bg-gray-100 transition-colors"
                        aria-label={isExpanded ? 'Collapse' : 'Expand'}
                      >
                        {isExpanded ? (
                          <ChevronUp className="h-5 w-5 text-gray-400" />
                        ) : (
                          <ChevronDown className="h-5 w-5 text-gray-400" />
                        )}
                      </button>

                      <div className="min-w-0">
                        <div className="flex items-center gap-2 flex-wrap">
                          <h3
                            className={cn(
                              'font-medium truncate',
                              area.isActive ? 'text-gray-900' : 'text-gray-500',
                            )}
                          >
                            {area.name}
                          </h3>

                          {/* ZIP count badge */}
                          {!isExpanded && (
                            <span className="inline-flex items-center rounded-full bg-gray-100 px-2 py-0.5 text-xs text-gray-500 font-medium whitespace-nowrap">
                              {area.zipCodes.length} ZIP{area.zipCodes.length !== 1 ? 's' : ''}
                            </span>
                          )}

                          {/* Empty zone warning */}
                          {isEmpty && (
                            <span
                              className="inline-flex items-center gap-1 text-xs text-warning font-medium"
                              title="No ZIP codes configured"
                            >
                              <AlertTriangle className="h-3 w-3" />
                              no ZIPs
                            </span>
                          )}
                        </div>

                        {area.description && (
                          <p className="mt-0.5 text-xs text-gray-400 truncate max-w-sm">
                            {area.description}
                          </p>
                        )}
                      </div>
                    </div>

                    {/* Right: toggle + actions */}
                    <div className="flex items-center gap-3 shrink-0 ml-4">
                      <div className="flex items-center gap-2">
                        <span className="text-xs text-gray-400 hidden sm:inline">
                          {area.isActive ? 'Active' : 'Inactive'}
                        </span>
                        <ToggleSwitch
                          checked={area.isActive}
                          onChange={(val) => handleToggleActive(area, val)}
                          disabled={isToggling}
                        />
                      </div>

                      <button
                        onClick={() => openEditModal(area)}
                        className="rounded-lg p-2 text-gray-400 hover:bg-gray-100 hover:text-gray-700 transition-colors"
                        title="Edit zone"
                      >
                        <Edit2 className="h-4 w-4" />
                      </button>
                      <button
                        onClick={() => setDeleteAreaId(area.id)}
                        className="rounded-lg p-2 text-gray-400 hover:bg-red-50 hover:text-danger transition-colors"
                        title="Delete zone"
                      >
                        <Trash2 className="h-4 w-4" />
                      </button>
                    </div>
                  </div>

                  {/* Expanded: ZIP codes + inline add input + city panel */}
                  {isExpanded && (
                    <div className="border-t border-gray-100 bg-gray-50 px-4 py-4">
                      {/* Row header: ZIP Codes label + "Add city ZIPs" button */}
                      <div className="flex items-center justify-between mb-3">
                        <p className="text-xs font-semibold uppercase tracking-wide text-gray-400">
                          ZIP Codes ({area.zipCodes.length})
                        </p>
                        <button
                          type="button"
                          onClick={() => toggleCityPanel(area.id)}
                          className="inline-flex items-center gap-1 rounded-lg border border-gray-300 px-2.5 py-1 text-xs font-medium text-gray-600 hover:bg-white hover:border-brand hover:text-brand transition-colors"
                        >
                          <Building2 className="h-3.5 w-3.5" />
                          {cityPanelOpen[area.id] ? 'Cancel' : 'Add city ZIPs'}
                        </button>
                      </div>

                      {/* "Add city ZIPs" inline panel */}
                      {cityPanelOpen[area.id] && (
                        <div className="mb-4 rounded-lg border border-brand/20 bg-white p-3">
                          <p className="text-xs font-medium text-gray-600 mb-2">
                            Bulk-add all ZIPs for a city
                          </p>
                          <CityFetchForm
                            value={cityPanelFetch[area.id] ?? defaultCityFetch()}
                            onChange={(next) =>
                              setCityPanelFetch((prev) => ({ ...prev, [area.id]: next }))
                            }
                            onConfirm={() => handleCityPanelConfirm(area.id)}
                            confirmLabel="Add ZIPs to zone"
                            confirmLoading={cityPanelAdding[area.id] ?? false}
                          />
                        </div>
                      )}

                      {/* City panel success message (shown after panel closes) */}
                      {cityPanelMsg[area.id] && !cityPanelOpen[area.id] && (
                        <p className="text-xs text-success font-medium mb-3">
                          {cityPanelMsg[area.id]}
                        </p>
                      )}

                      {/* ZIP chips */}
                      {area.zipCodes.length === 0 ? (
                        <p className="text-sm text-gray-400 italic mb-3">
                          No ZIP codes configured yet
                        </p>
                      ) : (
                        <div className="flex flex-wrap gap-2 mb-3">
                          {area.zipCodes.map((zip) => (
                            <div
                              key={zip.id}
                              className="group flex items-center gap-1 rounded-full bg-white border border-gray-200 px-3 py-1 text-sm shadow-sm"
                            >
                              <MapPin className="h-3 w-3 text-gray-300" />
                              <span className="font-mono text-gray-700">{zip.zipCode}</span>
                              <button
                                onClick={() =>
                                  setDeleteZip({
                                    areaId: area.id,
                                    zipId: zip.id,
                                    zipCode: zip.zipCode,
                                  })
                                }
                                className="ml-1 rounded-full p-0.5 text-gray-300 hover:bg-red-50 hover:text-danger opacity-0 group-hover:opacity-100 transition-opacity"
                                aria-label={`Remove ZIP ${zip.zipCode}`}
                              >
                                <X className="h-3 w-3" />
                              </button>
                            </div>
                          ))}
                        </div>
                      )}

                      {/* Inline ZIP add input */}
                      <div className="flex flex-col gap-1">
                        <div className="flex items-center gap-2">
                          <div className="relative flex-1 max-w-xs">
                            <input
                              ref={(el) => { zipInputRefs.current[area.id] = el; }}
                              type="text"
                              name="zipCode"
                              aria-label="Add ZIP code"
                              inputMode="numeric"
                              placeholder="Add ZIP (e.g. 33101) or paste multiple"
                              value={zipInputs[area.id] ?? ''}
                              onChange={(e) => handleZipInputChange(area.id, e.target.value)}
                              onKeyDown={(e) => handleZipKeyDown(e, area.id)}
                              onPaste={(e) => handleZipPaste(e, area.id)}
                              maxLength={5}
                              className={cn(
                                'w-full rounded-lg border px-3 py-1.5 text-sm font-mono placeholder:font-sans placeholder:text-gray-400 focus:outline-none focus:ring-1',
                                zipErrors[area.id]
                                  ? 'border-danger focus:border-danger focus:ring-danger/30'
                                  : 'border-gray-200 focus:border-brand focus:ring-brand/30',
                              )}
                            />
                            {zipAdding[area.id] && (
                              <span className="absolute right-2 top-1/2 -translate-y-1/2">
                                <Spinner size="sm" />
                              </span>
                            )}
                          </div>
                          <span className="text-xs text-gray-400 hidden sm:inline">
                            Press Enter to add
                          </span>
                        </div>

                        {zipErrors[area.id] && (
                          <p className="text-xs text-danger">{zipErrors[area.id]}</p>
                        )}

                        {zipBulkMsg[area.id] && (
                          <p className="text-xs text-success font-medium">{zipBulkMsg[area.id]}</p>
                        )}
                      </div>
                    </div>
                  )}
                </div>
              );
            })}
          </div>
        )}
      </div>

      {/* Delete Area Confirmation */}
      <AlertDialog
        open={deleteAreaId !== null}
        onOpenChange={(open) => !open && setDeleteAreaId(null)}
      >
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Delete service area?</AlertDialogTitle>
            <AlertDialogDescription>
              This will also delete all associated ZIP codes. This action cannot be undone.
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>Cancel</AlertDialogCancel>
            <AlertDialogAction
              onClick={() => deleteAreaId !== null && handleDelete(deleteAreaId)}
            >
              Delete
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>

      {/* Delete ZIP Code Confirmation */}
      <AlertDialog
        open={deleteZip !== null}
        onOpenChange={(open) => !open && setDeleteZip(null)}
      >
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Remove ZIP {deleteZip?.zipCode}?</AlertDialogTitle>
            <AlertDialogDescription>
              This action cannot be undone.
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>Cancel</AlertDialogCancel>
            <AlertDialogAction
              onClick={() =>
                deleteZip &&
                handleDeleteZipCode(deleteZip.areaId, deleteZip.zipId)
              }
            >
              Remove
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>

      {/* Create Zone Modal */}
      <Dialog open={modalMode === 'create'} onOpenChange={(open) => !open && handleCloseModal()}>
        <DialogContent className="max-w-lg">
          <DialogHeader>
            <DialogTitle>New Service Area</DialogTitle>
          </DialogHeader>

          <div className="space-y-5">
            <CityFetchForm
              value={createFetch}
              onChange={setCreateFetch}
            />

            {/* Description */}
            <div>
              <label htmlFor="create-zone-description" className="block text-sm font-medium text-gray-700 mb-1">
                Description
              </label>
              <textarea
                id="create-zone-description"
                name="description"
                value={createDescription}
                onChange={(e) => setCreateDescription(e.target.value)}
                placeholder="Optional zone description"
                rows={2}
                className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm focus:border-brand focus:outline-none focus:ring-1 focus:ring-brand"
              />
            </div>

            {/* Zone name preview */}
            {createFetch.city.trim() && createFetch.state.trim() && (
              <p className="text-xs text-gray-400">
                Zone will be named:{' '}
                <span className="font-medium text-gray-600">
                  {createFetch.city.trim()}, {createFetch.state.trim().toUpperCase()}
                </span>
              </p>
            )}

            {/* Actions */}
            <div className="flex justify-end gap-3 pt-2">
              <button
                type="button"
                onClick={handleCloseModal}
                className="rounded-lg border border-gray-300 px-4 py-2 text-sm font-medium text-gray-700 hover:bg-gray-50"
              >
                Cancel
              </button>
              <button
                type="button"
                disabled={
                  createSubmitting ||
                  createFetch.status !== 'success' ||
                  createFetch.zips.length === 0
                }
                onClick={handleCreateSubmit}
                className="inline-flex items-center gap-2 rounded-lg bg-brand px-4 py-2 text-sm font-medium text-white hover:bg-brand-dark disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
              >
                {createSubmitting ? (
                  <>
                    <Spinner size="sm" />
                    Creating…
                  </>
                ) : (
                  'Create Zone'
                )}
              </button>
            </div>
          </div>
        </DialogContent>
      </Dialog>

      {/* Edit Zone Modal */}
      <Dialog
        open={modalMode !== null && modalMode !== 'create'}
        onOpenChange={(open) => !open && handleCloseModal()}
      >
        <DialogContent className="max-w-md">
          <DialogHeader>
            <DialogTitle>Edit Zone</DialogTitle>
          </DialogHeader>

          <form onSubmit={handleEditSubmit} className="space-y-4">
            {editForm.formState.errors.root && (
              <p className="text-sm text-danger">{editForm.formState.errors.root.message}</p>
            )}

            <div>
              <label htmlFor="edit-zone-name" className="block text-sm font-medium text-gray-700 mb-1">
                Name *
              </label>
              <input
                id="edit-zone-name"
                type="text"
                {...editForm.register('name')}
                className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm focus:border-brand focus:outline-none focus:ring-1 focus:ring-brand"
              />
              {editForm.formState.errors.name && (
                <p className="mt-1 text-sm text-danger">
                  {editForm.formState.errors.name.message}
                </p>
              )}
            </div>

            <div>
              <label htmlFor="edit-zone-description" className="block text-sm font-medium text-gray-700 mb-1">
                Description
              </label>
              <textarea
                id="edit-zone-description"
                {...editForm.register('description')}
                rows={3}
                className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm focus:border-brand focus:outline-none focus:ring-1 focus:ring-brand"
              />
            </div>

            <div className="flex items-center gap-3">
              <input
                type="checkbox"
                id="editIsActive"
                {...editForm.register('isActive')}
                className="h-4 w-4 rounded border-gray-300 text-brand focus:ring-brand"
              />
              <label htmlFor="editIsActive" className="text-sm text-gray-700">
                Zone active
              </label>
            </div>

            <div className="flex justify-end gap-3 pt-4">
              <button
                type="button"
                onClick={handleCloseModal}
                className="rounded-lg border border-gray-300 px-4 py-2 text-sm font-medium text-gray-700 hover:bg-gray-50"
              >
                Cancel
              </button>
              <button
                type="submit"
                className="rounded-lg bg-brand px-4 py-2 text-sm font-medium text-white hover:bg-brand-dark"
              >
                Save Changes
              </button>
            </div>
          </form>
        </DialogContent>
      </Dialog>
    </AdminLayout>
  );
}
