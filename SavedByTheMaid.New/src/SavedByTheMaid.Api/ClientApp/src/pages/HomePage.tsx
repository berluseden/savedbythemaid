import { Link } from 'react-router-dom';
import { CheckCircle, Star, Clock, Shield, Sparkles, ArrowRight, Home, Leaf, Calendar } from 'lucide-react';
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
      <section className="relative overflow-hidden bg-gradient-to-br from-[#FFF9E0] via-white to-[#F0F7FF]">
        {/* Decorative background elements */}
        <div className="absolute inset-0 overflow-hidden">
          <div className="absolute -top-24 -right-24 h-96 w-96 rounded-full bg-[#FFE44D]/10 blur-3xl" />
          <div className="absolute -bottom-24 -left-24 h-96 w-96 rounded-full bg-[#00205B]/5 blur-3xl" />
        </div>

        <div className="relative mx-auto max-w-7xl px-4 py-20 sm:px-6 lg:px-8 lg:py-28">
          <div className="grid gap-12 lg:grid-cols-2 lg:gap-16 items-center">
            <div className="space-y-8">
              <div className="inline-flex items-center rounded-full bg-[#FFE44D]/20 px-4 py-1.5 text-sm font-medium text-[#001440] border border-[#FFE44D]/30">
                <Sparkles className="mr-2 h-4 w-4 text-[#E5C100]" />
                Professional Cleaning Services
              </div>

              <h1 className="text-4xl font-bold tracking-tight text-gray-900 sm:text-5xl lg:text-6xl leading-tight">
                A Clean Home,{' '}
                <span className="text-[#00205B]">Without the Work</span>
              </h1>

              <p className="text-lg text-gray-600 max-w-lg leading-relaxed">
                Book professional house cleaning in 60 seconds. Trusted cleaners,
                transparent pricing, and 100% satisfaction guaranteed.
              </p>

              {/* Key benefits */}
              <div className="flex flex-wrap gap-x-6 gap-y-3">
                <div className="flex items-center gap-2 text-sm text-gray-700">
                  <CheckCircle className="h-4 w-4 text-green-500 flex-shrink-0" />
                  Reliable service
                </div>
                <div className="flex items-center gap-2 text-sm text-gray-700">
                  <CheckCircle className="h-4 w-4 text-green-500 flex-shrink-0" />
                  Homes & offices
                </div>
                <div className="flex items-center gap-2 text-sm text-gray-700">
                  <CheckCircle className="h-4 w-4 text-green-500 flex-shrink-0" />
                  Attention to every detail
                </div>
              </div>

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
              <div className="flex items-center gap-6 pt-2">
                <div className="flex items-center gap-1">
                  {[...Array(5)].map((_, i) => (
                    <Star key={i} className="h-5 w-5 fill-yellow-400 text-yellow-400" />
                  ))}
                  <span className="ml-2 text-sm font-medium text-gray-600">4.9/5 (2,000+ reviews)</span>
                </div>
              </div>
            </div>

            {/* Hero Visual */}
            <div className="relative hidden lg:block">
              {/* Main illustration card */}
              <div className="relative rounded-2xl bg-gradient-to-br from-[#00205B] to-[#001440] p-8 shadow-2xl">
                <div className="space-y-6">
                  {/* Illustration header */}
                  <div className="flex items-center justify-between">
                    <div className="flex items-center gap-3">
                      <div className="flex h-10 w-10 items-center justify-center rounded-full bg-[#FFE44D]">
                        <Sparkles className="h-5 w-5 text-[#00205B]" />
                      </div>
                      <div>
                        <p className="text-sm font-semibold text-white">ecoMaid</p>
                        <p className="text-xs text-sky-200">Your cleaning partner</p>
                      </div>
                    </div>
                    <div className="flex items-center gap-1">
                      {[...Array(5)].map((_, i) => (
                        <Star key={i} className="h-3 w-3 fill-[#FFE44D] text-[#FFE44D]" />
                      ))}
                    </div>
                  </div>

                  {/* Service cards inside */}
                  <div className="grid grid-cols-2 gap-3">
                    <div className="rounded-xl bg-white/10 backdrop-blur-sm p-4 border border-white/10">
                      <Home className="h-6 w-6 text-[#FFE44D] mb-2" />
                      <p className="text-sm font-medium text-white">Home Cleaning</p>
                      <p className="text-xs text-sky-200 mt-1">From $99</p>
                    </div>
                    <div className="rounded-xl bg-white/10 backdrop-blur-sm p-4 border border-white/10">
                      <Sparkles className="h-6 w-6 text-[#FFE44D] mb-2" />
                      <p className="text-sm font-medium text-white">Deep Clean</p>
                      <p className="text-xs text-sky-200 mt-1">From $149</p>
                    </div>
                    <div className="rounded-xl bg-white/10 backdrop-blur-sm p-4 border border-white/10">
                      <Shield className="h-6 w-6 text-[#FFE44D] mb-2" />
                      <p className="text-sm font-medium text-white">Move In/Out</p>
                      <p className="text-xs text-sky-200 mt-1">From $199</p>
                    </div>
                    <div className="rounded-xl bg-white/10 backdrop-blur-sm p-4 border border-white/10">
                      <Leaf className="h-6 w-6 text-[#FFE44D] mb-2" />
                      <p className="text-sm font-medium text-white">Eco-Friendly</p>
                      <p className="text-xs text-sky-200 mt-1">100% Green</p>
                    </div>
                  </div>

                  {/* Bottom stat */}
                  <div className="flex items-center justify-between rounded-xl bg-[#FFE44D]/10 px-4 py-3 border border-[#FFE44D]/20">
                    <div>
                      <p className="text-sm font-semibold text-white">2,000+ happy clients</p>
                      <p className="text-xs text-sky-200">Serving homes & offices daily</p>
                    </div>
                    <ArrowRight className="h-5 w-5 text-[#FFE44D]" />
                  </div>
                </div>
              </div>

              {/* Floating card - booking confirmed */}
              <Card className="absolute -bottom-6 -left-6 p-4 shadow-lg animate-pulse-slow border-green-100">
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

              {/* Floating card - eco badge */}
              <Card className="absolute -top-4 -right-4 px-4 py-3 shadow-lg">
                <div className="flex items-center gap-2">
                  <Leaf className="h-5 w-5 text-green-500" />
                  <span className="text-sm font-medium text-gray-700">100% Eco-Friendly</span>
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
            <div className="inline-flex items-center rounded-full bg-[#00205B]/5 px-4 py-1.5 text-sm font-medium text-[#00205B] mb-4">
              Why ecoMaid?
            </div>
            <h2 className="text-3xl font-bold text-gray-900 sm:text-4xl">
              Everything you need for a spotless space
            </h2>
            <p className="mt-4 text-lg text-gray-600 max-w-2xl mx-auto">
              We make professional cleaning easy, affordable, and stress-free.
            </p>
          </div>

          <div className="grid gap-6 md:grid-cols-2 lg:grid-cols-4">
            {features.map((feature) => (
              <Card key={feature.title} hover className="p-6 text-center group">
                <div className="mx-auto mb-4 flex h-14 w-14 items-center justify-center rounded-xl bg-[#FFE44D]/20 group-hover:bg-[#FFE44D]/30 transition-colors">
                  <feature.icon className="h-7 w-7 text-[#00205B]" />
                </div>
                <h3 className="text-lg font-semibold text-gray-900">{feature.title}</h3>
                <p className="mt-2 text-sm text-gray-600 leading-relaxed">{feature.description}</p>
              </Card>
            ))}
          </div>
        </div>
      </section>

      {/* How It Works */}
      <section className="py-20 bg-gradient-to-b from-gray-50 to-white">
        <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
          <div className="text-center mb-16">
            <div className="inline-flex items-center rounded-full bg-[#FFE44D]/20 px-4 py-1.5 text-sm font-medium text-[#001440] mb-4">
              Simple process
            </div>
            <h2 className="text-3xl font-bold text-gray-900 sm:text-4xl">
              How It Works
            </h2>
            <p className="mt-4 text-lg text-gray-600">
              Getting your home cleaned is as easy as 1-2-3
            </p>
          </div>

          <div className="grid gap-8 md:grid-cols-3">
            {[
              { step: '1', title: 'Book Online', description: 'Choose your service, date, and time in our easy booking system.', icon: Calendar },
              { step: '2', title: 'We Clean', description: 'Our professional cleaners arrive on time and work their magic.', icon: Sparkles },
              { step: '3', title: 'Relax & Enjoy', description: 'Come home to a sparkling clean space. It\'s that simple!', icon: CheckCircle },
            ].map((item, index) => (
              <div key={item.step} className="relative text-center group">
                {/* Connector line between steps */}
                {index < 2 && (
                  <div className="hidden md:block absolute top-8 left-[60%] w-[80%] h-px bg-gradient-to-r from-[#00205B]/20 to-[#00205B]/20" />
                )}
                <div className="relative mx-auto mb-6 flex h-16 w-16 items-center justify-center rounded-2xl bg-[#00205B] text-white shadow-lg shadow-[#00205B]/20 group-hover:shadow-xl group-hover:shadow-[#00205B]/30 transition-shadow">
                  <item.icon className="h-7 w-7" />
                  <span className="absolute -top-2 -right-2 flex h-6 w-6 items-center justify-center rounded-full bg-[#FFE44D] text-xs font-bold text-[#00205B]">
                    {item.step}
                  </span>
                </div>
                <h3 className="text-xl font-semibold text-gray-900">{item.title}</h3>
                <p className="mt-2 text-gray-600 max-w-xs mx-auto">{item.description}</p>
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
      <section className="relative py-20 bg-[#00205B] overflow-hidden">
        <div className="absolute inset-0">
          <div className="absolute -top-40 -right-40 h-80 w-80 rounded-full bg-[#FFE44D]/10 blur-3xl" />
          <div className="absolute -bottom-40 -left-40 h-80 w-80 rounded-full bg-white/5 blur-3xl" />
        </div>
        <div className="relative mx-auto max-w-4xl px-4 text-center sm:px-6 lg:px-8">
          <h2 className="text-3xl font-bold text-white sm:text-4xl">
            Ready for a Cleaner Home?
          </h2>
          <p className="mt-4 text-lg text-sky-100 max-w-2xl mx-auto">
            Book your professional cleaning today and enjoy a spotless space tomorrow.
          </p>
          <div className="mt-8 flex flex-col sm:flex-row gap-4 justify-center">
            <Link to="/booking">
              <Button size="lg" variant="secondary" className="bg-[#FFE44D] text-[#00205B] hover:bg-[#FFD700] font-semibold">
                Book Now - It's Free to Schedule
                <ArrowRight className="ml-2 h-5 w-5" />
              </Button>
            </Link>
            <Link to="/contact">
              <Button size="lg" variant="outline" className="border-white/30 text-white hover:bg-white/10">
                Contact Us
              </Button>
            </Link>
          </div>
        </div>
      </section>
    </div>
  );
}
