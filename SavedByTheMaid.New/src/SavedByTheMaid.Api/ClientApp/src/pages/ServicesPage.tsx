import { Link } from 'react-router-dom';
import { Sparkles, Home, Building2, Truck, CheckCircle, Clock, Shield, Star } from 'lucide-react';

const services = [
  {
    id: 'standard',
    name: 'Standard Cleaning',
    description: 'Regular maintenance cleaning to keep your space fresh and tidy. Perfect for weekly or bi-weekly visits.',
    icon: Home,
    features: [
      'Dusting all surfaces',
      'Vacuuming and mopping floors',
      'Kitchen cleaning and sanitizing',
      'Bathroom cleaning and disinfecting',
      'Making beds and tidying rooms',
      'Trash removal',
    ],
    price: 'From $99',
    duration: '2-3 hours',
    popular: false,
  },
  {
    id: 'deep',
    name: 'Deep Cleaning',
    description: 'Thorough top-to-bottom cleaning for when your space needs extra attention. Recommended quarterly.',
    icon: Sparkles,
    features: [
      'Everything in Standard Cleaning',
      'Inside oven and refrigerator',
      'Inside windows and window sills',
      'Baseboards and door frames',
      'Light fixtures and ceiling fans',
      'Cabinet fronts and handles',
    ],
    price: 'From $199',
    duration: '4-6 hours',
    popular: true,
  },
  {
    id: 'moveinout',
    name: 'Move In/Out Cleaning',
    description: 'Complete cleaning for moving transitions. Leave your old place spotless or start fresh in your new home.',
    icon: Truck,
    features: [
      'Everything in Deep Cleaning',
      'Inside all cabinets and closets',
      'Appliance deep cleaning',
      'Wall spot cleaning',
      'Garage sweeping',
      'Final walkthrough inspection',
    ],
    price: 'From $299',
    duration: '6-8 hours',
    popular: false,
  },
  {
    id: 'office',
    name: 'Office Cleaning',
    description: 'Professional commercial cleaning for businesses of all sizes. Flexible scheduling available.',
    icon: Building2,
    features: [
      'Desk and workstation cleaning',
      'Reception and common areas',
      'Break room and kitchen sanitizing',
      'Restroom deep cleaning',
      'Floor care and carpet cleaning',
      'Trash and recycling removal',
    ],
    price: 'Custom Quote',
    duration: 'Varies',
    popular: false,
  },
];

const benefits = [
  {
    icon: Shield,
    title: 'Insured & Bonded',
    description: 'All our cleaners are fully insured and bonded for your peace of mind.',
  },
  {
    icon: Star,
    title: 'Vetted Professionals',
    description: 'Every cleaner passes background checks and extensive training.',
  },
  {
    icon: Clock,
    title: 'Flexible Scheduling',
    description: 'Book online 24/7 with same-day availability in most areas.',
  },
  {
    icon: CheckCircle,
    title: '100% Satisfaction',
    description: 'Not happy? We\'ll re-clean for free or refund your payment.',
  },
];

export default function ServicesPage() {
  return (
    <div className="min-h-screen bg-gray-50">
      {/* Hero Section */}
      <section className="bg-gradient-to-br from-[#FFE44D]/50 to-sky-600 text-white py-16">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 text-center">
          <h1 className="text-4xl md:text-5xl font-bold mb-4">Our Cleaning Services</h1>
          <p className="text-xl text-sky-100 max-w-2xl mx-auto">
            Professional cleaning tailored to your needs. From regular maintenance to deep cleaning,
            we've got you covered.
          </p>
        </div>
      </section>

      {/* Services Grid */}
      <section className="py-16">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="grid md:grid-cols-2 gap-8">
            {services.map((service) => (
              <div
                key={service.id}
                className={`relative bg-white rounded-2xl shadow-sm border ${
                  service.popular ? 'border-[#00205B] ring-2 ring-[#00205B]' : 'border-gray-200'
                } p-6 hover:shadow-lg transition-shadow`}
              >
                {service.popular && (
                  <div className="absolute -top-3 left-6 bg-[#00205B] text-white text-xs font-semibold px-3 py-1 rounded-full">
                    Most Popular
                  </div>
                )}
                <div className="flex items-start gap-4">
                  <div className="w-12 h-12 bg-[#FFE44D]/20 rounded-xl flex items-center justify-center flex-shrink-0">
                    <service.icon className="w-6 h-6 text-[#00205B]" />
                  </div>
                  <div className="flex-1">
                    <h3 className="text-xl font-semibold text-gray-900">{service.name}</h3>
                    <p className="text-gray-600 mt-1">{service.description}</p>
                  </div>
                </div>

                <div className="mt-6">
                  <div className="flex items-center justify-between mb-4">
                    <div>
                      <span className="text-2xl font-bold text-gray-900">{service.price}</span>
                    </div>
                    <div className="flex items-center text-gray-500 text-sm">
                      <Clock className="w-4 h-4 mr-1" />
                      {service.duration}
                    </div>
                  </div>

                  <ul className="space-y-2 mb-6">
                    {service.features.map((feature, idx) => (
                      <li key={idx} className="flex items-start gap-2 text-sm text-gray-600">
                        <CheckCircle className="w-4 h-4 text-green-500 flex-shrink-0 mt-0.5" />
                        {feature}
                      </li>
                    ))}
                  </ul>

                  <Link
                    to="/booking"
                    className={`block w-full text-center py-3 rounded-lg font-medium transition-colors ${
                      service.popular
                        ? 'bg-[#00205B] text-white hover:bg-[#001440]'
                        : 'bg-gray-100 text-gray-900 hover:bg-gray-200'
                    }`}
                  >
                    Book {service.name}
                  </Link>
                </div>
              </div>
            ))}
          </div>
        </div>
      </section>

      {/* Benefits Section */}
      <section className="py-16 bg-white">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="text-center mb-12">
            <h2 className="text-3xl font-bold text-gray-900">¿Por qué elegir ecoMaid?</h2>
            <p className="text-gray-600 mt-2">We're committed to delivering exceptional cleaning services</p>
          </div>

          <div className="grid sm:grid-cols-2 lg:grid-cols-4 gap-8">
            {benefits.map((benefit, idx) => (
              <div key={idx} className="text-center">
                <div className="w-14 h-14 bg-[#FFE44D]/20 rounded-2xl flex items-center justify-center mx-auto mb-4">
                  <benefit.icon className="w-7 h-7 text-[#00205B]" />
                </div>
                <h3 className="font-semibold text-gray-900 mb-2">{benefit.title}</h3>
                <p className="text-sm text-gray-600">{benefit.description}</p>
              </div>
            ))}
          </div>
        </div>
      </section>

      {/* CTA Section */}
      <section className="py-16 bg-gradient-to-br from-[#FFE44D]/50 to-sky-600">
        <div className="max-w-4xl mx-auto px-4 sm:px-6 lg:px-8 text-center">
          <h2 className="text-3xl font-bold text-white mb-4">Ready to Get Started?</h2>
          <p className="text-sky-100 mb-8">
            Book your first cleaning in under 60 seconds. No contracts, no hassle.
          </p>
          <Link
            to="/booking"
            className="inline-flex items-center gap-2 bg-white text-[#00205B] px-8 py-4 rounded-xl font-semibold hover:bg-gray-100 transition-colors"
          >
            <Sparkles className="w-5 h-5" />
            Book Your Cleaning Now
          </Link>
        </div>
      </section>
    </div>
  );
}
