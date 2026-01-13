import { Link } from 'react-router-dom';
import { CheckCircle, Star, Clock, Shield, Sparkles, ArrowRight } from 'lucide-react';
import { Button, Card } from '@/components/ui';

export default function HomePage() {
  const features = [
    {
      icon: Clock,
      title: 'Book in 60 Seconds',
      description: 'Quick and easy online booking. Choose your date, time, and services.',
    },
    {
      icon: Shield,
      title: 'Trusted Professionals',
      description: 'Background-checked and trained cleaning experts you can trust.',
    },
    {
      icon: Star,
      title: '5-Star Service',
      description: '100% satisfaction guaranteed. We\'re not happy until you are.',
    },
    {
      icon: CheckCircle,
      title: 'Eco-Friendly',
      description: 'We use green cleaning products that are safe for your family.',
    },
  ];

  return (
    <div>
      {/* Hero Section */}
      <section className="relative overflow-hidden bg-gradient-to-br from-[#FFE44D]/5 to-white">
        <div className="mx-auto max-w-7xl px-4 py-20 sm:px-6 lg:px-8 lg:py-28">
          <div className="grid gap-12 lg:grid-cols-2 lg:gap-8 items-center">
            <div className="space-y-8">
              <div className="inline-flex items-center rounded-full bg-[#FFE44D]/20 px-4 py-1.5 text-sm font-medium text-[#001440]">
                <Sparkles className="mr-2 h-4 w-4" />
                Professional Cleaning Services
              </div>
              
              <h1 className="text-4xl font-bold tracking-tight text-gray-900 sm:text-5xl lg:text-6xl">
                A Clean Home,{' '}
                <span className="text-[#00205B]">Without the Work</span>
              </h1>
              
              <p className="text-lg text-gray-600 max-w-lg">
                Book professional house cleaning in 60 seconds. Trusted cleaners, 
                transparent pricing, and 100% satisfaction guaranteed.
              </p>

              <div className="flex flex-col sm:flex-row gap-4">
                <Link to="/booking">
                  <Button size="lg" className="w-full sm:w-auto">
                    Book Your Cleaning
                    <ArrowRight className="ml-2 h-5 w-5" />
                  </Button>
                </Link>
                <Link to="/services">
                  <Button variant="outline" size="lg" className="w-full sm:w-auto">
                    View Services
                  </Button>
                </Link>
              </div>

              {/* Trust badges */}
              <div className="flex items-center gap-6 pt-4">
                <div className="flex items-center gap-1">
                  {[...Array(5)].map((_, i) => (
                    <Star key={i} className="h-5 w-5 fill-yellow-400 text-yellow-400" />
                  ))}
                  <span className="ml-2 text-sm font-medium text-gray-600">4.9/5 (2,000+ reviews)</span>
                </div>
              </div>
            </div>

            {/* Hero Image Placeholder */}
            <div className="relative">
              <div className="aspect-square rounded-2xl bg-gradient-to-br from-[#FFE44D]/20 to-[#FFE44D]/30 flex items-center justify-center">
                {/* 3 Bubbles like favicon */}
                <svg viewBox="0 0 100 100" fill="none" className="h-40 w-40">
                  {/* Large bubble (bottom right) */}
                  <circle cx="58" cy="65" r="24" fill="#00205B"/>
                  <ellipse cx="50" cy="55" rx="7" ry="10" fill="white" opacity="0.35"/>
                  
                  {/* Medium bubble (top right) */}
                  <circle cx="72" cy="32" r="16" fill="#00205B"/>
                  <ellipse cx="66" cy="26" rx="5" ry="7" fill="white" opacity="0.35"/>
                  
                  {/* Small bubble (top left) */}
                  <circle cx="38" cy="28" r="12" fill="#00205B"/>
                  <ellipse cx="34" cy="24" rx="4" ry="5" fill="white" opacity="0.35"/>
                  
                  {/* Yellow sparkle */}
                  <g transform="translate(18, 50)">
                    <path d="M0 -6 L0 6 M-6 0 L6 0" stroke="#F7C52D" strokeWidth="2.5" strokeLinecap="round"/>
                    <path d="M-4 -4 L4 4 M4 -4 L-4 4" stroke="#F7C52D" strokeWidth="1.5" strokeLinecap="round"/>
                  </g>
                  
                  {/* Small sparkle */}
                  <g transform="translate(28, 78)">
                    <path d="M0 -3 L0 3 M-3 0 L3 0" stroke="#F7C52D" strokeWidth="1.5" strokeLinecap="round"/>
                  </g>
                </svg>
              </div>
              
              {/* Floating card */}
              <Card className="absolute -bottom-6 -left-6 p-4 shadow-lg">
                <div className="flex items-center gap-3">
                  <div className="flex h-12 w-12 items-center justify-center rounded-full bg-green-100">
                    <CheckCircle className="h-6 w-6 text-green-600" />
                  </div>
                  <div>
                    <p className="font-semibold text-gray-900">Booking Confirmed!</p>
                    <p className="text-sm text-gray-500">Tomorrow at 9:00 AM</p>
                  </div>
                </div>
              </Card>
            </div>
          </div>
        </div>
      </section>

      {/* Features Section */}
      <section className="py-20 bg-white">
        <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
          <div className="text-center mb-16">
            <h2 className="text-3xl font-bold text-gray-900 sm:text-4xl">
              ¿Por qué elegir ecoMaid?
            </h2>
            <p className="mt-4 text-lg text-gray-600 max-w-2xl mx-auto">
              We make professional cleaning easy, affordable, and stress-free.
            </p>
          </div>

          <div className="grid gap-8 md:grid-cols-2 lg:grid-cols-4">
            {features.map((feature) => (
              <Card key={feature.title} className="p-6 text-center">
                <div className="mx-auto mb-4 flex h-14 w-14 items-center justify-center rounded-xl bg-[#FFE44D]/20">
                  <feature.icon className="h-7 w-7 text-[#00205B]" />
                </div>
                <h3 className="text-lg font-semibold text-gray-900">{feature.title}</h3>
                <p className="mt-2 text-sm text-gray-600">{feature.description}</p>
              </Card>
            ))}
          </div>
        </div>
      </section>

      {/* How It Works */}
      <section className="py-20 bg-gray-50">
        <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
          <div className="text-center mb-16">
            <h2 className="text-3xl font-bold text-gray-900 sm:text-4xl">
              How It Works
            </h2>
            <p className="mt-4 text-lg text-gray-600">
              Getting your home cleaned is as easy as 1-2-3
            </p>
          </div>

          <div className="grid gap-8 md:grid-cols-3">
            {[
              { step: '1', title: 'Book Online', description: 'Choose your service, date, and time in our easy booking system.' },
              { step: '2', title: 'We Clean', description: 'Our professional cleaners arrive on time and work their magic.' },
              { step: '3', title: 'Relax & Enjoy', description: 'Come home to a sparkling clean space. It\'s that simple!' },
            ].map((item) => (
              <div key={item.step} className="relative text-center">
                <div className="mx-auto mb-4 flex h-16 w-16 items-center justify-center rounded-full bg-[#00205B] text-2xl font-bold text-white">
                  {item.step}
                </div>
                <h3 className="text-xl font-semibold text-gray-900">{item.title}</h3>
                <p className="mt-2 text-gray-600">{item.description}</p>
              </div>
            ))}
          </div>

          <div className="mt-12 text-center">
            <Link to="/booking">
              <Button size="lg">
                Get Started Now
                <ArrowRight className="ml-2 h-5 w-5" />
              </Button>
            </Link>
          </div>
        </div>
      </section>

      {/* CTA Section */}
      <section className="py-20 bg-[#00205B]">
        <div className="mx-auto max-w-4xl px-4 text-center sm:px-6 lg:px-8">
          <h2 className="text-3xl font-bold text-white sm:text-4xl">
            Ready for a Cleaner Home?
          </h2>
          <p className="mt-4 text-lg text-sky-100">
            Book your professional cleaning today and enjoy a spotless space tomorrow.
          </p>
          <div className="mt-8">
            <Link to="/booking">
              <Button size="lg" variant="secondary" className="bg-white text-[#00205B] hover:bg-[#FFE44D]/10">
                Book Now - It's Free to Schedule
                <ArrowRight className="ml-2 h-5 w-5" />
              </Button>
            </Link>
          </div>
        </div>
      </section>
    </div>
  );
}
