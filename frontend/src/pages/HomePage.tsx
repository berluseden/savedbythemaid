import { Link } from 'react-router-dom';
import { CheckCircle, Star, Clock, Shield, Sparkles, ArrowRight, Home, Calendar } from 'lucide-react';
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
      {/* Hero */}
      <section className="relative overflow-hidden bg-white">
        {/* Subtle dot-grid background */}
        <div
          className="absolute inset-0 opacity-[0.03]"
          style={{
            backgroundImage: 'radial-gradient(circle, #2196f3 1px, transparent 1px)',
            backgroundSize: '28px 28px',
          }}
        />
        {/* Soft glow blobs */}
        <div className="absolute -top-32 -right-32 h-96 w-96 rounded-full bg-brand/8 blur-3xl" />
        <div className="absolute -bottom-32 -left-32 h-96 w-96 rounded-full bg-brand-50 blur-3xl" />

        <div className="relative mx-auto max-w-3xl px-4 py-20 text-center sm:px-6 lg:px-8">
          {/* Badge */}
          <div className="mb-6 inline-flex items-center gap-2 rounded-full border border-brand/20 bg-brand-50 px-4 py-1.5 text-sm font-medium text-brand">
            <Sparkles className="h-3.5 w-3.5" />
            Professional Cleaning Service
          </div>

          <h1 className="text-4xl font-extrabold tracking-tight text-gray-900 sm:text-5xl lg:text-6xl">
            Welcome to a Cleaner,{' '}
            <span className="text-brand">Fresher Space!</span>
          </h1>

          <p className="mt-6 text-lg text-gray-500 leading-relaxed max-w-2xl mx-auto">
            We believe a clean space creates peace, comfort, and happiness. Our professional
            cleaning team is here to make your home or business shine while you relax and enjoy
            what matters most.
          </p>

          {/* Pill badges */}
          <div className="mt-8 flex flex-wrap items-center justify-center gap-3">
            <span className="inline-flex items-center gap-1.5 rounded-full border border-gray-200 bg-white px-4 py-1.5 text-sm font-medium text-gray-700 shadow-sm">
              <CheckCircle className="h-4 w-4 text-green-500" />
              Reliable service
            </span>
            <span className="inline-flex items-center gap-1.5 rounded-full border border-gray-200 bg-white px-4 py-1.5 text-sm font-medium text-gray-700 shadow-sm">
              <Home className="h-4 w-4 text-brand" />
              Homes & offices
            </span>
            <span className="inline-flex items-center gap-1.5 rounded-full border border-gray-200 bg-white px-4 py-1.5 text-sm font-medium text-gray-700 shadow-sm">
              <Sparkles className="h-4 w-4 text-brand" />
              Attention to every detail
            </span>
          </div>

          <p className="mt-8 text-lg text-gray-500">
            Let us take care of the cleaning so you can enjoy a spotless space every day!
          </p>
          <p className="mt-3 text-lg font-semibold text-brand">
            Contact us today to schedule your cleaning. Your sparkling space starts here!
          </p>

          <div className="mt-8 flex flex-col sm:flex-row gap-3 justify-center">
            <Link to="/booking">
              <Button size="lg" className="w-full sm:w-auto shadow-md shadow-brand/20">
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

          {/* Stars */}
          <div className="mt-8 flex items-center justify-center gap-1">
            {[...Array(5)].map((_, i) => (
              <Star key={i} className="h-4 w-4 fill-yellow-400 text-yellow-400" />
            ))}
            <span className="ml-2 text-sm font-medium text-gray-500">4.9/5 (2,000+ reviews)</span>
          </div>
        </div>
      </section>

      {/* Features */}
      <section className="py-20 bg-gray-50">
        <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
          <div className="text-center mb-14">
            <div className="inline-flex items-center rounded-full border border-brand/20 bg-brand-50 px-4 py-1.5 text-sm font-medium text-brand mb-4">
              Why ecoMaid?
            </div>
            <h2 className="text-3xl font-bold tracking-tight text-gray-900 sm:text-4xl">
              Everything you need for a spotless space
            </h2>
            <p className="mt-4 text-lg text-gray-500 max-w-2xl mx-auto">
              We make professional cleaning easy, affordable, and stress-free.
            </p>
          </div>

          <div className="grid gap-5 md:grid-cols-2 lg:grid-cols-4">
            {features.map((feature) => (
              <Card
                key={feature.title}
                hover
                className="p-6 bg-white border border-gray-100 shadow-none hover:shadow-md hover:-translate-y-0.5 transition-all duration-200"
              >
                <div className="mb-4 flex h-10 w-10 items-center justify-center rounded-xl bg-brand-50">
                  <feature.icon className="h-5 w-5 text-brand" />
                </div>
                <h3 className="text-base font-semibold text-gray-900">{feature.title}</h3>
                <p className="mt-2 text-sm text-gray-500 leading-relaxed">{feature.description}</p>
              </Card>
            ))}
          </div>
        </div>
      </section>

      {/* How It Works */}
      <section className="py-20 bg-white">
        <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
          <div className="text-center mb-14">
            <div className="inline-flex items-center rounded-full border border-gray-200 bg-gray-50 px-4 py-1.5 text-sm font-medium text-gray-600 mb-4">
              Simple process
            </div>
            <h2 className="text-3xl font-bold tracking-tight text-gray-900 sm:text-4xl">
              How It Works
            </h2>
            <p className="mt-4 text-lg text-gray-500">
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
                {/* Dashed connector */}
                {index < 2 && (
                  <div className="hidden md:block absolute top-8 left-[60%] w-[80%] h-px border-t-2 border-dashed border-gray-200" />
                )}
                {/* Icon circle */}
                <div className="relative mx-auto mb-6 inline-flex">
                  <div className="flex h-16 w-16 items-center justify-center rounded-2xl bg-brand text-white shadow-lg shadow-brand/25 group-hover:shadow-xl group-hover:shadow-brand/35 transition-shadow">
                    <item.icon className="h-7 w-7" />
                  </div>
                  <span className="absolute -top-2 -right-2 flex h-6 w-6 items-center justify-center rounded-full bg-brand-dark text-xs font-bold text-white ring-2 ring-white">
                    {item.step}
                  </span>
                </div>
                <h3 className="text-xl font-semibold text-gray-900">{item.title}</h3>
                <p className="mt-2 text-gray-500 max-w-xs mx-auto text-sm leading-relaxed">{item.description}</p>
              </div>
            ))}
          </div>

          <div className="mt-12 text-center">
            <Link to="/booking">
              <Button size="lg" className="shadow-md shadow-brand/20">
                Get Started Now
                <ArrowRight className="ml-2 h-5 w-5" />
              </Button>
            </Link>
          </div>
        </div>
      </section>

      {/* CTA */}
      <section className="relative py-20 bg-brand-dark overflow-hidden">
        <div className="absolute inset-0 opacity-[0.06]"
          style={{
            backgroundImage: 'radial-gradient(circle, #fff 1px, transparent 1px)',
            backgroundSize: '24px 24px',
          }}
        />
        <div className="absolute -top-40 -right-40 h-80 w-80 rounded-full bg-brand/20 blur-3xl" />
        <div className="absolute -bottom-40 -left-40 h-80 w-80 rounded-full bg-white/5 blur-3xl" />
        <div className="relative mx-auto max-w-4xl px-4 text-center sm:px-6 lg:px-8">
          <h2 className="text-3xl font-bold tracking-tight text-white sm:text-4xl">
            Ready for a Cleaner Home?
          </h2>
          <p className="mt-4 text-lg text-blue-200 max-w-2xl mx-auto">
            Book your professional cleaning today and enjoy a spotless space tomorrow.
          </p>
          <div className="mt-8 flex flex-col sm:flex-row gap-3 justify-center">
            <Link to="/booking">
              <Button size="lg" className="bg-white text-brand hover:bg-gray-50 font-semibold shadow-lg">
                Book Now — It's Free to Schedule
                <ArrowRight className="ml-2 h-5 w-5" />
              </Button>
            </Link>
            <Link to="/contact">
              <Button size="lg" variant="outline" className="border-white/20 text-white hover:bg-white/10">
                Contact Us
              </Button>
            </Link>
          </div>
        </div>
      </section>
    </div>
  );
}
